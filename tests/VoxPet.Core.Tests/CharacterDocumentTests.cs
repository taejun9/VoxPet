using System.Text.Json;
using VoxPet.Core.Actions;
using VoxPet.Core.Character;
using VoxPet.Core.Editing;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class CharacterDocumentTests
{
    private static VoxPetPackage Sample()
    {
        string folder = Path.Combine(AppContext.BaseDirectory, "ImageSetTemplate");
        var set = JsonSerializer.Deserialize<ImageSet>(File.ReadAllText(Path.Combine(folder, "imageset.json")), ImageSet.JsonOptions)!;
        var definition = new LegacyImageSetSource(set).Load();
        var manifest = new VoxPetManifest("test.editor", "Editor", "1.2.3", "QA", "original", character: "data/character.json", thumbnail: definition.Parts[0].Asset,
            actions: definition.Actions.Select((a, i) => $"custom/action-{i}.json"), attachments: definition.Attachments.Select((a, i) => $"custom/attachment-{i}.json"));
        var assets = set.Files.ToDictionary(p => p, p => File.ReadAllBytes(Path.Combine(folder, p))); assets.Add("unused.png", assets.Values.First().ToArray());
        return new VoxPetPackageValidator().Validate(manifest, definition, assets);
    }
    [Fact]
    public void NoOpAndMetadataEditsPreserveEverySupportedFieldPathAndAsset()
    {
        var original = Sample(); var doc = new CharacterDocument(original); Assert.Equal(new VoxPetPackageExporter().Export(original), doc.Edit(_ => { }).Export());
        var edited = doc.Metadata(original.Manifest.Id, "Edited", "2.0", original.Manifest.Author, "changed", original.Manifest.Thumbnail, 768, 512);
        var loaded = new VoxPetPackageLoader().Load(new MemoryStream(edited.Export())); Assert.Equal("Edited", loaded.Manifest.Name); Assert.Equal(768, loaded.Character.CanvasWidth);
        Assert.Equal(original.Manifest.Character, loaded.Manifest.Character); Assert.Equal(original.Manifest.Actions, loaded.Manifest.Actions); Assert.Equal(original.Manifest.Attachments, loaded.Manifest.Attachments);
        Assert.Equal(original.Character.Actions.Select(a => a.DurationMs), loaded.Character.Actions.Select(a => a.DurationMs)); Assert.Equal(original.Character.Poses.Count, loaded.Character.Poses.Count);
        foreach (string path in original.AssetPaths) Assert.Equal(original.ReadAsset(path), loaded.ReadAsset(path)); Assert.Contains("unused.png", loaded.AssetPaths);
        Assert.Equal("Editor", doc.Package.Manifest.Name);
    }
    [Fact]
    public void PartVisibilityRoundTripsAndSuppressesDependentFaceWithoutLosingBytes()
    {
        var doc = new CharacterDocument(Sample()); var edited = doc.Edit(d => { int i = d.Parts.FindIndex(p => p.Id == "head"); var p = d.Parts[i]; d.Parts[i] = new(p.Id, p.Type, p.Asset, p.Anchor, p.ZIndex, p.Transform, p.Side, visible: false); });
        var loaded = new VoxPetPackageLoader().Load(new MemoryStream(edited.Export())); Assert.False(loaded.Character.Parts.Single(p => p.Id == "head").Visible);
        var frame = loaded.CreateRuntime().Update(new(0, 0, 0)); Assert.DoesNotContain(frame.Layers, l => l.Id == "part-head" || l.Id.StartsWith("face-"));
        Assert.Equal(doc.Package.ReadAsset("head.png"), loaded.ReadAsset("head.png"));
    }
    [Fact]
    public void FailedParentCycleAndMissingAssetTransactionsKeepOriginalSnapshot()
    {
        var doc = new CharacterDocument(Sample()); byte[] before = doc.Export();
        Assert.Throws<ArgumentException>(() => doc.Edit(d => { d.Anchors[0] = new("root", parent: "head"); }));
        Assert.Throws<VoxPetPackageException>(() => doc.Edit(d => { var p = d.Parts[0]; d.Parts[0] = new(p.Id, p.Type, "missing.png", p.Anchor); }));
        Assert.Equal(before, doc.Export());
    }
    [Fact]
    public void AddRemoveActionCreatesReferencesWhileKeepingExistingActionPayloads()
    {
        var doc = new CharacterDocument(Sample()); var next = doc.Edit(d => d.Actions.Add(new("new-action", "New", 1000, [new(0, ActionEventKind.Transform, "head", transform: new(x: 12))])));
        var loaded = new VoxPetPackageLoader().Load(new MemoryStream(next.Export())); Assert.Contains("actions/new-action.json", loaded.Manifest.Actions);
        Assert.Equal(doc.Package.Character.Actions.Count + 1, loaded.Character.Actions.Count); Assert.Equal(doc.Package.Manifest.Actions, loaded.Manifest.Actions.Take(doc.Package.Manifest.Actions.Count));
        Assert.Equal(doc.Export(), next.Edit(d => d.Actions.RemoveAll(a => a.Id == "new-action")).Export());
    }
}
