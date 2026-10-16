using System.IO;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;

namespace VoxPet.App.Services;

/// <summary>공개 의상만 읽는다. 개인 시트는 읽거나 복사하지 않는다.</summary>
internal static class CompanionSampleLibrary
{
    internal const string LegacySampleId = "voxpet.samples.studio";
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Lazy<Task<LoadedCharacterPackage?>>> cache = new();
    internal static string PathFor(string id) => Path.Combine(AppContext.BaseDirectory, "Assets", "Samples", "companion-" + CharacterCatalog.NormalizeId(id) + ".voxpet");
    internal static async Task<LoadedCharacterPackage?> LoadAsync(string id)
    {
        id = CharacterCatalog.NormalizeId(id);
        var pending = cache.GetOrAdd(id, key => new(() => ReadAsync(key)));
        var result = await pending.Value;
        if (result == null) cache.TryRemove(new KeyValuePair<string, Lazy<Task<LoadedCharacterPackage?>>>(id, pending));
        return result;
    }
    private static async Task<LoadedCharacterPackage?> ReadAsync(string id)
    {
        try { return await Task.Run(() => CharacterPackageLibrary.Decode(new VoxPetPackageLoader().Load(PathFor(id)))); }
        catch (Exception error) when (CharacterPackageLibrary.Expected(error)) { return null; }
    }
}
