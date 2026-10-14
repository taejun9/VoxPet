using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using VoxPet.Core.Triggers;
using Xunit;
using CharacterDefinition = VoxPet.Core.Character.CharacterDefinition;

namespace VoxPet.Core.Tests;

public sealed class VoxPetPackageTests
{
    private static byte[] Png() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "ImageSetTemplate", "body.png"));
    private static Dictionary<string, byte[]> MinimalFiles() => new()
    {
        ["manifest.json"] = Encoding.UTF8.GetBytes("{\"format\":\"voxpet\",\"formatVersion\":2,\"id\":\"test\",\"name\":\"시험\",\"version\":\"1.0.0\"}"),
        ["character.json"] = Encoding.UTF8.GetBytes("{\"schemaVersion\":2,\"parts\":[{\"id\":\"body\",\"type\":\"body\",\"file\":\"body.png\"}]}"),
        ["body.png"] = Png()
    };
    private static byte[] Zip(IEnumerable<KeyValuePair<string, byte[]>> files, bool symlink = false)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            foreach (var pair in files)
            {
                var entry = zip.CreateEntry(pair.Key); if (symlink && pair.Key == "body.png") entry.ExternalAttributes = unchecked((int)0xa1ff0000);
                using var content = entry.Open(); content.Write(pair.Value);
            }
        return stream.ToArray();
    }
    private static VoxPetPackage Load(byte[] bytes, VoxPetPackageLimits? limits = null) => new VoxPetPackageLoader(limits).Load(new MemoryStream(bytes, false));
    [Fact]
    public void MinimalCharacterNeedsNoActionsAttachmentsOrThumbnail()
    {
        var package = Load(Zip(MinimalFiles())); Assert.Equal(2, package.Manifest.FormatVersion); Assert.Equal("1.0.0", package.Manifest.Version);
        Assert.Empty(package.Character.Actions); Assert.Empty(package.Character.Attachments); Assert.Null(package.Manifest.Thumbnail);
        Assert.Single(package.CreateRuntime().Update(new(0, 0, 0)).Layers);
        var original = Png(); var returned = package.ReadAsset("body.png"); returned[0] = 0; Assert.Equal(original, package.ReadAsset("body.png"));
        Assert.Equal(original, Load(new VoxPetPackageExporter().Export(package)).ReadAsset("body.png"));
    }
    [Fact]
    public void LegacyModelAndAllSupportedDataRoundTripWithoutLosingBytesOrFrameOutput()
    {
        var old = JsonSerializer.Deserialize<ImageSet>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ImageSetTemplate", "imageset.json")), ImageSet.JsonOptions)!;
        var definition = LegacyImageSetAdapter.Convert(old);
        var assets = old.Files.ToDictionary(path => path, path => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "ImageSetTemplate", path)));
        var manifest = new VoxPetManifest("com.test.character", "시험 캐릭터", "2026.10", "author", "설명\n두 줄", thumbnail: "body.png",
            actions: ["actions/fire.json"], attachments: ["attachments/outfit.json", "attachments/weapon.json"]);
        var package = new VoxPetPackageValidator().Validate(manifest, definition, assets); var loaded = Load(new VoxPetPackageExporter().Export(package));
        Assert.Equal(PackageJson.WriteManifest(manifest), PackageJson.WriteManifest(loaded.Manifest));
        Assert.Equal(definition.Assets.Order(), loaded.Character.Assets.Order());
        Assert.Equal(definition.Actions[0].Timeline.Select(t => (t.TimeMs, t.Kind, t.Value)), loaded.Character.Actions[0].Timeline.Select(t => (t.TimeMs, t.Kind, t.Value)));
        Assert.Equal(definition.Poses.Select(p => p.Name), loaded.Character.Poses.Select(p => p.Name));
        Assert.Equal(definition.Motion, loaded.Character.Motion); Assert.Equal(definition.Face, loaded.Character.Face);
        foreach (var asset in assets) Assert.Equal(asset.Value, loaded.ReadAsset(asset.Key));
        var a = package.CreateRuntime(new Random(1)); var b = loaded.CreateRuntime(new Random(1));
        foreach (var runtime in new[] { a, b }) { runtime.Attachments.SetEnabled("outfit-0", true); runtime.Attachments.SetEnabled("weapon-0", true); runtime.RequestAction("action-0"); }
        foreach (double time in new[] { 0, .081, .161, .241, .341 })
        {
            var first = a.Update(new(time, 0, .8, false)); var second = b.Update(new(time, 0, .8, false));
            Assert.Equal(first.Layers.Select(l => (l.Asset, l.Matrix, l.Width, l.Height)), second.Layers.Select(l => (l.Asset, l.Matrix, l.Width, l.Height)));
        }
    }
    [Fact]
    public void GenericEventsTriggersAttachmentInheritanceAndPlacementRoundTrip()
    {
        var attachment = new AttachmentDefinition("hat", "모자", "hat", [new("layer", "body.png", "root", 320,
            inherit: new(false, true, false, true), placement: new(new(2, 3, .8, 1.2, 40), AttachmentSpace.Anchor, .7, 30, 40))]);
        var action = new ActionDefinition("action", "동작", 1000, [new(0, ActionEventKind.Transform, "body", transform: new(2, 3, 1.2, .8, 4)),
            new(10, ActionEventKind.Visibility, "body", enabled: false), new(20, ActionEventKind.Sprite, "body", "body.png"),
            new(30, ActionEventKind.Effect, "root", "body.png", durationMs: 40), new(50, ActionEventKind.Sound, value: "reserved.wav"),
            new(60, ActionEventKind.Attachment, value: "hat")], 80, new(ActionPlaybackMode.Loop, false, false),
            triggers: [new("key", "action", TriggerKind.Hotkey, "1", modifiers: TriggerModifiers.Ctrl | TriggerModifiers.Alt), new("voice", "action", TriggerKind.VoiceThreshold, threshold: .7)]);
        var c = new CharacterDefinition([new("body", "body", "body.png", "root")], [new("root")], canvasWidth: 1200, canvasHeight: 675, actions: [action], attachments: [attachment]);
        var manifest = new VoxPetManifest("test", "시험", "2", actions: ["actions/action.json"], attachments: ["attachments/hat.json"]);
        var loaded = Load(new VoxPetPackageExporter().Export(new VoxPetPackageValidator().Validate(manifest, c, new Dictionary<string, byte[]> { ["body.png"] = Png() })));
        Assert.Equal(action.Timeline, loaded.Character.Actions[0].Timeline); Assert.Equal(action.Triggers, loaded.Character.Actions[0].Triggers);
        Assert.Equal(attachment.Layers[0].Placement, loaded.Character.Attachments[0].Layers[0].Placement);
        Assert.Equal(attachment.Layers[0].Inherit, loaded.Character.Attachments[0].Layers[0].Inherit);
        Assert.Equal(1200, loaded.Character.CanvasWidth); Assert.Equal(675, loaded.Character.CanvasHeight);
    }
    [Theory]
    [InlineData("../escape.png")]
    [InlineData("/absolute.png")]
    [InlineData("C:/drive.png")]
    [InlineData("a\\evil.png")]
    [InlineData("a/../evil.png")]
    [InlineData("a//evil.png")]
    [InlineData("a/./evil.png")]
    [InlineData("NUL.png")]
    [InlineData("COM¹.png")]
    [InlineData("folder./evil.png")]
    [InlineData("folder /evil.png")]
    [InlineData("payload.exe")]
    public void UntrustedArchivePathsAreRejected(string path)
    {
        var files = MinimalFiles(); files.Add(path, Png()); Assert.Throws<VoxPetPackageException>(() => Load(Zip(files)));
    }
    [Fact]
    public void DuplicateCaseNormalizedUnicodeAndSymlinkEntriesAreRejected()
    {
        var files = MinimalFiles().ToList(); files.Add(new("BODY.PNG", Png())); Assert.Throws<VoxPetPackageException>(() => Load(Zip(files)));
        files = MinimalFiles().ToList(); files.Add(new("é.png", Png())); files.Add(new("e\u0301.png", Png())); Assert.Throws<VoxPetPackageException>(() => Load(Zip(files)));
        Assert.Throws<VoxPetPackageException>(() => Load(Zip(MinimalFiles(), true)));
        files = MinimalFiles().ToList(); files.Add(new("parts/", [])); Assert.NotNull(Load(Zip(files)));
    }
    [Fact]
    public void ArchiveAssetJsonEntryDecodedAndTotalLimitsAreEnforced()
    {
        byte[] zip = Zip(MinimalFiles());
        Assert.Throws<VoxPetPackageException>(() => Load(zip, new(ArchiveBytes: zip.Length - 1)));
        Assert.Throws<VoxPetPackageException>(() => Load(zip, new(Entries: 2)));
        Assert.Throws<VoxPetPackageException>(() => Load(zip, new(AssetBytes: Png().Length - 1)));
        Assert.Throws<VoxPetPackageException>(() => Load(zip, new(JsonBytes: 10)));
        Assert.Throws<VoxPetPackageException>(() => Load(zip, new(TotalBytes: 100)));
        Assert.Throws<VoxPetPackageException>(() => Load(zip, new(DecodedBytes: 100)));
        Assert.Throws<VoxPetPackageException>(() => Load(zip, new(PngDimension: 256)));
    }
    [Fact]
    public void MissingReferencesBadJsonDuplicatePropertiesAndMalformedPngFail()
    {
        var files = MinimalFiles(); files.Remove("body.png"); Assert.Throws<VoxPetPackageException>(() => Load(Zip(files)));
        files = MinimalFiles(); files["character.json"] = "broken"u8.ToArray(); Assert.Throws<VoxPetPackageException>(() => Load(Zip(files)));
        files = MinimalFiles(); files["character.json"] = "{\"schemaVersion\":2,\"SchemaVersion\":2}"u8.ToArray(); Assert.Throws<VoxPetPackageException>(() => Load(Zip(files)));
        files = MinimalFiles(); files["body.png"][16] ^= 1; Assert.Throws<VoxPetPackageException>(() => Load(Zip(files)));
        files = MinimalFiles(); files["body.png"] = Png()[..40]; Assert.Throws<VoxPetPackageException>(() => Load(Zip(files)));
        files = MinimalFiles(); files["manifest.json"] = Encoding.UTF8.GetBytes("{\"format\":\"voxpet\",\"formatVersion\":3}"); Assert.Throws<VoxPetPackageException>(() => Load(Zip(files)));
    }
    [Fact]
    public void PngInflationCannotExceedItsDeclaredScanlines()
    {
        var files = MinimalFiles(); var png = files["body.png"];
        BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(16), 1); BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(20), 1);
        uint crc = uint.MaxValue;
        foreach (byte value in png.AsSpan(12, 17))
        {
            crc ^= value; for (int i = 0; i < 8; i++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0);
        }
        BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(29), ~crc);
        Assert.Throws<VoxPetPackageException>(() => Load(Zip(files)));
    }
    [Fact]
    public void CentralDirectoryCountsAndCrcCannotLieAboutActualPayload()
    {
        var bytes = Zip(MinimalFiles()); int end = bytes.Length - 22;
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(end + 8), 1); BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(end + 10), 1);
        Assert.Throws<VoxPetPackageException>(() => Load(bytes));
        bytes = Zip(MinimalFiles()); end = bytes.Length - 22; int start = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(end + 16)); bytes[start + 16] ^= 1;
        Assert.Throws<VoxPetPackageException>(() => Load(bytes));
        bytes = Zip(MinimalFiles()); BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(bytes.Length - 12), ushort.MaxValue); Assert.Throws<VoxPetPackageException>(() => Load(bytes));
    }
    [Fact]
    public void NativeAttachmentInventorySurvivesRestartAndCorruptionDoesNotOverwrite()
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-native-inventory-" + Guid.NewGuid());
        try
        {
            var attachment = new AttachmentDefinition("hat", "모자", "hat", [new("hat", "hat.png", "root", placement: new(width: 100, height: 80))]);
            var store = new AttachmentStore(folder); store.SaveNative(new([attachment], ["hat"])); var loaded = store.LoadNative();
            Assert.False(loaded.Recovered); Assert.Equal(new[] { "hat" }, loaded.EnabledIds); Assert.Equal(attachment.Layers[0].Placement, loaded.Definitions[0].Layers[0].Placement);
            string path = Path.Combine(folder, "attachments-v2.json"); File.WriteAllText(path, "broken"); Assert.True(store.LoadNative().Recovered); Assert.Equal("broken", File.ReadAllText(path));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
