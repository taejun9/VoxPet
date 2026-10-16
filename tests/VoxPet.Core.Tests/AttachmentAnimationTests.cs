using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Packages;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class AttachmentAnimationTests
{
    [Theory]
    [InlineData(0, "a.png")]
    [InlineData(.124, "a.png")]
    [InlineData(.125, "b.png")]
    [InlineData(.249, "b.png")]
    [InlineData(.25, "a.png")]
    [InlineData(.375, "b.png")]
    public void FrameBoundariesAndLoopUseRuntimeTime(double time, string expected)
        => Assert.Equal(expected, new AttachmentAnimation(["a.png", "b.png"], 125).FrameAt(time));

    [Fact]
    public void DefinitionCopiesFramesBoundsWorkAndRejectsInvalidTimeOrPaths()
    {
        string[] frames = ["a.png", "b.png"]; var animation = new AttachmentAnimation(frames); frames[0] = "changed.png";
        Assert.Equal("a.png", animation.Frames[0]); Assert.Contains(animation.FrameAt(double.MaxValue), animation.Frames);
        foreach (double time in new[] { -1, double.NaN, double.PositiveInfinity }) Assert.Throws<ArgumentOutOfRangeException>(() => animation.FrameAt(time));
        foreach (int count in new[] { 0, 1, 65 }) Assert.Throws<ArgumentException>(() => new AttachmentAnimation(Enumerable.Repeat("a.png", count)));
        foreach (double ms in new[] { 0, 15.9, 10000.1, double.NaN, double.PositiveInfinity }) Assert.Throws<ArgumentOutOfRangeException>(() => new AttachmentAnimation(["a.png", "b.png"], ms));
        Assert.Throws<ArgumentException>(() => new AttachmentAnimation(["../a.png", "b.png"]));
        Assert.Throws<ArgumentException>(() => new AttachmentAnimation([null!, "b.png"]));
        Assert.Equal(64, new AttachmentAnimation(Enumerable.Repeat("a.png", 64), 16).Frames.Count);
    }

    [Fact]
    public void StaticAndPoseRulesRemainCompatibleAndPoseOverridesTheAnimation()
    {
        var rig = new VoxPet.Core.Motion.RigMotionSolver(new RigDefinition([new("root")])).Resolve();
        AttachmentSystem System(AttachmentLayer layer) => new([new("hat", "Hat", "hat", [layer])]);
        string Resolve(AttachmentLayer layer) { var s = System(layer); s.SetEnabled("hat", true); return s.Resolve(rig, time: .15).Single().Asset; }
        var animation = new AttachmentAnimation(["a.png", "b.png"]);
        Assert.Equal("still.png", Resolve(new("image", "still.png")));
        Assert.Equal("b.png", Resolve(new("image", "still.png", animation: animation)));
        Assert.Equal("pose.png", Resolve(new("image", "still.png", poses: new Dictionary<string, string> { ["idle"] = "pose.png" }, animation: animation)));
        Assert.Equal("b.png", Resolve(new("image", "still.png", poses: new Dictionary<string, string> { ["other"] = "pose.png" }, animation: animation)));
        var hidden = System(new("image", "still.png", poses: new Dictionary<string, string> { ["other"] = "pose.png" }, hideOnMissingPose: true, animation: animation));
        hidden.SetEnabled("hat", true); Assert.Empty(hidden.Resolve(rig, time: .15));
    }

    [Fact]
    public void PackageAndInventoryRoundTripAnimationAndValidateEveryReferencedFrame()
    {
        byte[] png = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "ImageSetTemplate", "body.png"));
        var a = new AttachmentDefinition("hat", "Hat", "hat", [new("image", "body.png", animation: new(["a.png", "b.png"], 80))]);
        var definition = new VoxPet.Core.Character.CharacterDefinition([new("body", "body", "body.png", "root")], [new("root")], attachments: [a]);
        var manifest = new VoxPetManifest("animation.test", "Animation", "1.0", attachments: ["attachments/hat.json"]);
        var assets = new Dictionary<string, byte[]> { ["body.png"] = png, ["a.png"] = png, ["b.png"] = png };
        var package = new VoxPetPackageValidator().Validate(manifest, definition, assets);
        var read = new VoxPetPackageLoader().Load(new MemoryStream(new VoxPetPackageExporter().Export(package)));
        Assert.Equal(a.Layers[0].Animation!.Frames, read.Character.Attachments[0].Layers[0].Animation!.Frames);
        Assert.Equal(80, read.Character.Attachments[0].Layers[0].Animation!.FrameDurationMs);
        assets.Remove("b.png"); Assert.Throws<VoxPetPackageException>(() => new VoxPetPackageValidator().Validate(manifest, definition, assets));
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-animation-" + Guid.NewGuid());
        try { var store = new AttachmentStore(folder); store.SaveNative(new([a], ["hat"])); Assert.Equal(a.Layers[0].Animation!.Frames, store.LoadNative().Definitions[0].Layers[0].Animation!.Frames); }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        var legacy = PackageJson.ReadAttachment(System.Text.Encoding.UTF8.GetBytes("{\"schemaVersion\":2,\"id\":\"hat\",\"name\":\"Hat\",\"category\":\"hat\",\"layers\":[{\"file\":\"body.png\"}]}"));
        Assert.Null(legacy.Layers[0].Animation);
        Assert.Throws<VoxPetPackageException>(() => PackageJson.ReadAttachment(System.Text.Encoding.UTF8.GetBytes("{\"schemaVersion\":2,\"id\":\"hat\",\"name\":\"Hat\",\"category\":\"hat\",\"layers\":[{\"file\":\"body.png\",\"animation\":[]}]}")));
    }
}
