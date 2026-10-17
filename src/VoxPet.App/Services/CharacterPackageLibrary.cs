using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Packages;

namespace VoxPet.App.Services;

public sealed record LoadedCharacterPackage(string LibraryId, VoxPetPackage Package, IReadOnlyDictionary<string, ImageSource> Images);
public sealed record CharacterPackageEntry(string LibraryId, string Name, string CharacterId, string Version, string Author);
/// <summary>worker 전용 보관함. ZIP는 추출하지 않고 원본 bytes와 마지막 선택을 atomic 저장한다.</summary>
public sealed class CharacterPackageLibrary(string folder)
{
    public int SkippedCount { get; private set; }
    private string FilePath(string id)
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("보관함ID를 확인하세요.");
        return Path.Combine(folder, id + ".voxpet");
    }
    public static LoadedCharacterPackage Decode(VoxPetPackage package, string id = "")
    {
        var images = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in package.AssetPaths)
        {
            using var stream = new MemoryStream(package.ReadAsset(path), false);
            var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count != 1) throw new VoxPetPackageException("PNG는 단일 프레임이어야 합니다.");
            var bitmap = decoder.Frames[0]; var dimensions = package.Dimensions[path];
            if (bitmap.PixelWidth != dimensions.Width || bitmap.PixelHeight != dimensions.Height) throw new VoxPetPackageException("PNG 디코딩 크기가 다릅니다.");
            bitmap.Freeze(); images.Add(path, bitmap);
        }
        return new(id, package, images);
    }
    public LoadedCharacterPackage Load(string id) => Decode(new VoxPetPackageLoader().Load(FilePath(id)), id);
    public LoadedCharacterPackage Import(string source)
    {
        if (Directory.Exists(folder) && Directory.EnumerateFiles(folder, "*.voxpet").Take(100).Count() >= 100) throw new VoxPetPackageException("보관함은100개까지 등록합니다.");
        using var input = File.OpenRead(source);
        if (input.Length > new VoxPetPackageLimits().ArchiveBytes) throw new VoxPetPackageException("패키지 크기 제한입니다.");
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[16 * 1024]; int read;
        while ((read = input.Read(chunk)) != 0)
        {
            if (buffer.Length + read > new VoxPetPackageLimits().ArchiveBytes) throw new VoxPetPackageException("패키지 크기 제한입니다.");
            buffer.Write(chunk, 0, read);
        }
        if (buffer.Length > new VoxPetPackageLimits().ArchiveBytes) throw new VoxPetPackageException("패키지 크기 제한입니다.");
        buffer.Position = 0; var package = new VoxPetPackageLoader().Load(buffer);
        string id = Guid.NewGuid().ToString("N"); var loaded = Decode(package, id);
        package.CreateRuntime().Update(new(0, 0, 0));
        // decode 성공 뒤에만 관리 복사본을 쓴다. 사용자 원본은 수정하지 않는다.
        Directory.CreateDirectory(folder); Atomic(FilePath(id), buffer.ToArray()); return loaded;
    }
    public IReadOnlyList<CharacterPackageEntry> List()
    {
        SkippedCount = 0; var entries = new List<CharacterPackageEntry>(); if (!Directory.Exists(folder)) return entries;
        foreach (string path in Directory.EnumerateFiles(folder, "*.voxpet").Take(101))
        {
            if (entries.Count >= 100) break;
            string id = Path.GetFileNameWithoutExtension(path); if (!Guid.TryParseExact(id, "N", out _)) continue;
            try { var manifest = new VoxPetPackageLoader().Load(path).Manifest; entries.Add(new(id, manifest.Name, manifest.Id, manifest.Version, manifest.Author)); }
            catch (Exception e) when (Expected(e)) { SkippedCount++; /* 원본을 보존하고 UI에 누락을 알린다. */ }
        }
        return entries;
    }
    public string? Selected()
    {
        string path = Path.Combine(folder, "selection.json"); if (!File.Exists(path)) return null;
        try
        {
            if (new FileInfo(path).Length > 4096) return null;
            string? id = JsonSerializer.Deserialize<string>(File.ReadAllText(path));
            return id != null && Guid.TryParseExact(id, "N", out _) ? id : null;
        }
        catch (Exception e) when (Expected(e)) { return null; }
    }
    public void Select(string? id)
    {
        if (id != null) _ = FilePath(id);
        Directory.CreateDirectory(folder); Atomic(Path.Combine(folder, "selection.json"), JsonSerializer.SerializeToUtf8Bytes(id));
    }
    public void Delete(string id) { if (Selected() == id) Select(null); File.Delete(FilePath(id)); }
    private static void Atomic(string path, byte[] bytes)
    {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(temporary, bytes); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    internal static bool Expected(Exception e) => e is VoxPetPackageException or IOException or UnauthorizedAccessException or ArgumentException or JsonException or FormatException or NotSupportedException or OverflowException or System.Runtime.InteropServices.COMException;
}
