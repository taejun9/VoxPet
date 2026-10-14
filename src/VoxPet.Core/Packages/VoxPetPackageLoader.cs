using System.Buffers.Binary;
using System.IO.Compression;
using System.Text.Json;

namespace VoxPet.Core.Packages;

public sealed class VoxPetPackageLoader
{
    private readonly VoxPetPackageValidator validator;
    public VoxPetPackageLoader(VoxPetPackageLimits? limits = null) => validator = new(limits);
    public VoxPetPackage Load(string path)
    {
        using var file = File.OpenRead(path); return Load(file);
    }
    public VoxPetPackage Load(Stream source)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(source); byte[] bytes = ReadBounded(source, validator.Limits.ArchiveBytes); var checksums = Preflight(bytes, validator.Limits.Entries);
            using var memory = new MemoryStream(bytes, false); using var zip = new ZipArchive(memory, ZipArchiveMode.Read);
            if (zip.Entries.Count > validator.Limits.Entries) throw new VoxPetPackageException("ZIP entry 수 상한입니다.");
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase); long total = 0; int entryIndex = 0;
            foreach (var entry in zip.Entries)
            {
                uint expectedCrc = checksums[entryIndex++];
                bool directory = entry.FullName.EndsWith('/'); string name = PackagePath.Normalize(entry.FullName, directory);
                if (!names.Add(name) || ((entry.ExternalAttributes >> 16) & 0xf000) == 0xa000) throw new VoxPetPackageException("중복/링크 ZIP entry입니다.");
                if (directory) { if (entry.Length != 0) throw new VoxPetPackageException("directory data는 허용하지 않습니다."); continue; }
                long maximum = name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? validator.Limits.JsonBytes : validator.Limits.AssetBytes;
                if (entry.Length > maximum || entry.Length > validator.Limits.TotalBytes - total) throw new VoxPetPackageException("ZIP 해제 크기 상한입니다.");
                using var content = entry.Open(); byte[] payload = ReadBounded(content, maximum);
                if (PngPackageValidator.Crc(payload) != expectedCrc || payload.LongLength != entry.Length) throw new VoxPetPackageException("ZIP CRC/실제 길이가 다릅니다.");
                total += payload.Length;
                if (total > validator.Limits.TotalBytes) throw new VoxPetPackageException("실제 ZIP 해제 합계 상한입니다.");
                if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) using (PackageJson.Parse(payload)) { }
                files.Add(name, payload);
            }
            byte[] Document(string path) => files.TryGetValue(PackagePath.Normalize(path), out var data) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? data : throw new VoxPetPackageException("JSON 문서가 없습니다: " + path);
            var manifest = PackageJson.ReadManifest(Document("manifest.json"));
            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "manifest.json" };
            foreach (string path in new[] { manifest.Character }.Concat(manifest.Actions).Concat(manifest.Attachments))
                if (!referenced.Add(PackagePath.Normalize(path))) throw new VoxPetPackageException("JSON 문서 참조 중복입니다.");
            var actions = manifest.Actions.Select(path => PackageJson.ReadAction(Document(path))).ToArray();
            var attachments = manifest.Attachments.Select(path => PackageJson.ReadAttachment(Document(path))).ToArray();
            var character = PackageJson.ReadCharacter(Document(manifest.Character), actions, attachments);
            return validator.Validate(manifest, character, files.Where(p => p.Key.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception error) when (error is ArgumentException or JsonException or InvalidOperationException or InvalidDataException or OverflowException or EndOfStreamException or NotSupportedException)
        { throw new VoxPetPackageException(".voxpet ZIP/JSON을 읽지 못했습니다.", error); }
    }
    internal static byte[] ReadBounded(Stream source, long maximum)
    {
        using var output = new MemoryStream(); byte[] buffer = new byte[8192]; int read;
        while ((read = source.Read(buffer)) > 0)
        { if (output.Length > maximum - read) throw new VoxPetPackageException("실제 읽기 크기 상한입니다."); output.Write(buffer, 0, read); }
        return output.ToArray();
    }
    private static IReadOnlyList<uint> Preflight(ReadOnlySpan<byte> bytes, int maximumEntries)
    {
        for (int offset = bytes.Length - 22; offset >= Math.Max(0, bytes.Length - 65557); offset--)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]) != 0x06054b50) continue;
            if (offset + 22 + BinaryPrimitives.ReadUInt16LittleEndian(bytes[(offset + 20)..]) != bytes.Length) continue;
            ushort disk = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(offset + 4)..]), startDisk = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(offset + 6)..]);
            ushort onDisk = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(offset + 8)..]), count = BinaryPrimitives.ReadUInt16LittleEndian(bytes[(offset + 10)..]);
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 12)..]), start = BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset + 16)..]);
            if (disk != 0 || startDisk != 0 || onDisk != count || count == ushort.MaxValue || count > maximumEntries || size == uint.MaxValue || start == uint.MaxValue ||
                (long)start + size > offset || offset >= 20 && BinaryPrimitives.ReadUInt32LittleEndian(bytes[(offset - 20)..]) == 0x07064b50)
                throw new VoxPetPackageException("ZIP64/multi-volume/central directory 상한 또는 구조 오류입니다.");
            var checksums = new List<uint>(); long end = (long)start + size; long position = start;
            while (position < end)
            {
                if (end - position < 46 || checksums.Count >= maximumEntries) throw new VoxPetPackageException("central directory entry 상한/구조 오류입니다.");
                var record = bytes[(int)position..];
                if (BinaryPrimitives.ReadUInt32LittleEndian(record) != 0x02014b50 || (BinaryPrimitives.ReadUInt16LittleEndian(record[8..]) & 1) != 0 ||
                    BinaryPrimitives.ReadUInt16LittleEndian(record[10..]) is not (0 or 8) || BinaryPrimitives.ReadUInt16LittleEndian(record[34..]) != 0 ||
                    BinaryPrimitives.ReadUInt32LittleEndian(record[20..]) == uint.MaxValue || BinaryPrimitives.ReadUInt32LittleEndian(record[24..]) == uint.MaxValue)
                    throw new VoxPetPackageException("encrypted/unsupported ZIP entry입니다.");
                long length = 46L + BinaryPrimitives.ReadUInt16LittleEndian(record[28..]) + BinaryPrimitives.ReadUInt16LittleEndian(record[30..]) + BinaryPrimitives.ReadUInt16LittleEndian(record[32..]);
                if (length > end - position) throw new VoxPetPackageException("central directory가 잘렸습니다.");
                uint local = BinaryPrimitives.ReadUInt32LittleEndian(record[42..]);
                if ((long)local + 30 > start || BinaryPrimitives.ReadUInt32LittleEndian(bytes[(int)local..]) != 0x04034b50) throw new VoxPetPackageException("local ZIP header 오류입니다.");
                checksums.Add(BinaryPrimitives.ReadUInt32LittleEndian(record[16..])); position += length;
            }
            if (checksums.Count != count) throw new VoxPetPackageException("ZIP entry count가 다릅니다.");
            return checksums;
        }
        throw new VoxPetPackageException("ZIP end record가 없습니다.");
    }
}
