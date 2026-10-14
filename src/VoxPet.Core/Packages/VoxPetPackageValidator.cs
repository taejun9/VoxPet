using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Motion;
using VoxPet.Core.Rendering;

namespace VoxPet.Core.Packages;

public sealed class VoxPetPackageValidator
{
    public VoxPetPackageLimits Limits { get; }
    public VoxPetPackageValidator(VoxPetPackageLimits? limits = null) { Limits = limits ?? new(); Limits.Validate(); }
    public VoxPetPackage Validate(VoxPetManifest manifest, CharacterDefinition character, IReadOnlyDictionary<string, byte[]> assets)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(manifest); ArgumentNullException.ThrowIfNull(character); ArgumentNullException.ThrowIfNull(assets);
            new RigMotionSolver(character.Rig); new AttachmentSystem(character.Attachments);
            var owned = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase); var sizes = new Dictionary<string, AssetDimensions>(StringComparer.OrdinalIgnoreCase);
            long total = 0, decoded = 0;
            foreach (var pair in assets)
            {
                string path = PackagePath.Normalize(pair.Key); ArgumentNullException.ThrowIfNull(pair.Value);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || pair.Value.Length > Limits.AssetBytes || !owned.TryAdd(path, pair.Value))
                    throw new VoxPetPackageException("PNG 자산 크기/중복을 확인하세요.");
                total += pair.Value.Length; var dimensions = PngPackageValidator.Validate(pair.Value, Limits); decoded += (long)dimensions.Width * (long)dimensions.Height * 4;
                if (total > Limits.TotalBytes || decoded > Limits.DecodedBytes) throw new VoxPetPackageException("이미지 합계 상한을 초과했습니다.");
                sizes.Add(path, dimensions);
            }
            if (owned.Count + 2 + character.Actions.Count + character.Attachments.Count > Limits.Entries) throw new VoxPetPackageException("파일 수 상한입니다.");
            foreach (string path in character.Assets.Concat(manifest.Thumbnail is null ? [] : new[] { manifest.Thumbnail }))
                if (!owned.ContainsKey(PackagePath.Normalize(path))) throw new VoxPetPackageException("참조 PNG가 없습니다: " + path);
            if (manifest.Actions.Count != character.Actions.Count || manifest.Attachments.Count != character.Attachments.Count) throw new VoxPetPackageException("문서 참조 개수가 다릅니다.");
            return new(manifest, character, owned, sizes);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or OverflowException or InvalidDataException or EndOfStreamException)
        { throw new VoxPetPackageException("패키지 정의/자산 검증에 실패했습니다.", error); }
    }
}
