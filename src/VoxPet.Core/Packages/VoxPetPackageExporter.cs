using System.IO.Compression;

namespace VoxPet.Core.Packages;

public sealed class VoxPetPackageExporter
{
    private readonly VoxPetPackageLimits limits;
    public VoxPetPackageExporter(VoxPetPackageLimits? limits = null) { this.limits = limits ?? new(); this.limits.Validate(); }
    public byte[] Export(VoxPetPackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Add(string path, byte[] bytes)
            {
                path = PackagePath.Normalize(path); if (!used.Add(path)) throw new VoxPetPackageException("내보내기 경로 중복입니다.");
                if (bytes.Length > (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? limits.JsonBytes : limits.AssetBytes)) throw new VoxPetPackageException("내보내기 문서/자산 상한입니다.");
                var entry = archive.CreateEntry(path, CompressionLevel.Optimal); entry.LastWriteTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
                using var content = entry.Open(); content.Write(bytes);
            }
            Add("manifest.json", PackageJson.WriteManifest(package.Manifest)); Add(package.Manifest.Character, PackageJson.WriteCharacter(package.Character));
            for (int i = 0; i < package.Character.Actions.Count; i++) Add(package.Manifest.Actions[i], PackageJson.WriteAction(package.Character.Actions[i]));
            for (int i = 0; i < package.Character.Attachments.Count; i++) Add(package.Manifest.Attachments[i], PackageJson.WriteAttachment(package.Character.Attachments[i]));
            foreach (string asset in package.AssetPaths.Order(StringComparer.Ordinal)) Add(asset, package.ReadAsset(asset));
        }
        if (memory.Length > limits.ArchiveBytes) throw new VoxPetPackageException("내보내기 ZIP 상한입니다.");
        byte[] result = memory.ToArray(); new VoxPetPackageLoader(limits).Load(new MemoryStream(result, false)); return result;
    }
    public void Export(VoxPetPackage package, string path)
    {
        byte[] bytes = Export(package); string full = Path.GetFullPath(path), directory = Path.GetDirectoryName(full)!; Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, ".voxpet-" + Guid.NewGuid().ToString("N") + ".tmp");
        try { File.WriteAllBytes(temporary, bytes); File.Move(temporary, full, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
