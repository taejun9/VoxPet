using System.Collections.ObjectModel;
using VoxPet.Core.Character;
using VoxPet.Core.Rendering;

namespace VoxPet.Core.Packages;

public sealed class VoxPetPackage
{
    public VoxPetManifest Manifest { get; }
    public CharacterDefinition Character { get; }
    private readonly IReadOnlyDictionary<string, byte[]> assets;
    public IReadOnlyDictionary<string, AssetDimensions> Dimensions { get; }
    public IEnumerable<string> AssetPaths => assets.Keys;
    internal VoxPetPackage(VoxPetManifest manifest, CharacterDefinition character, IReadOnlyDictionary<string, byte[]> assets, IReadOnlyDictionary<string, AssetDimensions> dimensions)
    {
        Manifest = manifest; Character = character;
        this.assets = new ReadOnlyDictionary<string, byte[]>(assets.ToDictionary(p => p.Key, p => p.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
        Dimensions = new ReadOnlyDictionary<string, AssetDimensions>(dimensions.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase));
    }
    public byte[] ReadAsset(string path) => assets.TryGetValue(PackagePath.Normalize(path), out var bytes) ? bytes.ToArray() : throw new VoxPetPackageException("자산이 없습니다.");
    public CharacterRuntime CreateRuntime(Random? random = null, double startedAt = 0) => new(Character, random, dimensions: Dimensions, startedAt: startedAt,
        geometry: Character.Attachments.Any(a => a.Layers.Any(l => l.Placement.DeformWithRig)) ? new RigRenderGeometryProvider(Character) : null);
}
public sealed class VoxPetPackageSource(VoxPetPackage package) : ICharacterDefinitionSource
{
    public CharacterDefinition Load() => package.Character;
}
