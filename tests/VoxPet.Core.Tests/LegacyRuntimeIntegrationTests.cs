using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class LegacyRuntimeIntegrationTests
{
    [Fact]
    public void FullSpriteSelectionTransitionTearsAndViewportUseCentralRuntime()
    {
        var legacy = new LegacySpriteSheetRuntime();
        var runtime = new CharacterRuntime(new LegacySpriteSheetSource(3).Load(), processor: legacy);
        legacy.Parameters = new(.7, 6, 0, 0, 0, MouthState.Open);
        legacy.Apply(ExpressionProfile.Default(2), 3, 10);
        var start = runtime.Update(new(10, 0, .7)); Assert.Equal("legacy/sprite-2-0.png", legacy.Sprite);
        Assert.Equal(0, start.Layers.Single(l => l.Id == "legacy-sprite").Opacity); Assert.Equal(-6, start.ViewportTransform.Y);
        var middle = runtime.Update(new(10.12, .12, .7)); Assert.Equal(.5, legacy.Blend, 8);
        Assert.Contains(middle.Layers, l => l.Id == "legacy-previous" && Math.Abs(l.Opacity - .5) < 1e-8);
        runtime.Update(new(10.3, .18, .7)); Assert.False(legacy.HasPrevious); Assert.Equal(1, legacy.Blend);
        var tears = runtime.Update(new(10.5, .2, 0)); Assert.Contains(tears.Layers, l => l.Id.StartsWith("legacy-tear-") && l.Opacity > 0);
        legacy.Apply(ExpressionProfile.Default(0) with { Blink = false }, 8, 11);
        legacy.Parameters = legacy.Parameters with { MouthOpen = .90, EyeOpen = 0 };
        runtime.Update(new(11.3, .8, .9)); Assert.Equal("legacy/sprite-7-1.png", legacy.Sprite);
    }
    [Theory]
    [InlineData(3, .6, 2)]
    [InlineData(8, .90, 7)]
    public void SpriteSheetSourceIsPlayableWithoutLegacyProcessor(int count, double voice, int mouth)
    {
        var runtime = new CharacterRuntime(new LegacySpriteSheetSource(count).Load(), new Random(1));
        var frame = runtime.Update(new(1, 1, voice, blink: true));
        Assert.Equal(mouth, runtime.MouthFrame); Assert.Single(frame.Layers);
        Assert.Equal(LegacySpriteSheetAdapter.Asset(mouth, 0), frame.Layers[0].Asset);
        var awake = runtime.Update(new(2, 1, voice, blink: false)); Assert.Equal(LegacySpriteSheetAdapter.Asset(mouth, 1), awake.Layers[0].Asset);
    }
    [Fact]
    public void FullSpriteBankSurvivesPackageCodecWithoutSeparateFaceLayers()
    {
        var definition = new LegacySpriteSheetSource(8, ExpressionProfile.Default(1) with { Blink = false }).Load();
        byte[] png = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "ImageSetTemplate", "body.png"));
        var assets = definition.Assets.ToDictionary(path => path, _ => png);
        Assert.Equal(16, assets.Count);
        var package = new VoxPetPackageValidator().Validate(new("sheet", "Full Sprite", "1"), definition, assets);
        byte[] archive = new VoxPetPackageExporter().Export(package);
        var loaded = new VoxPetPackageLoader().Load(new MemoryStream(archive));
        Assert.Equal("legacy8", loaded.Character.SpriteSheet!.Curve); Assert.False(loaded.Character.SpriteSheet.Expressions[0].Blink);
        var runtime = loaded.CreateRuntime(); var frame = runtime.Update(new(1, 1, 1, blink: true));
        Assert.Single(frame.Layers); Assert.Equal("legacy/sprite-7-1.png", frame.Layers[0].Asset);
        foreach (string path in loaded.AssetPaths) Assert.Equal(png, loaded.ReadAsset(path));
        Assert.Equal(archive, new VoxPetPackageExporter().Export(loaded));
    }
    [Fact]
    public void SharedActionEngineOwnsLegacyHandLifetime()
    {
        var legacy = new LegacySpriteSheetRuntime(); var runtime = new CharacterRuntime(new LegacySpriteSheetSource(3).Load(), processor: legacy);
        var scheduler = new HandMotionScheduler(runtime.Actions); var settings = HandMotionSettings.Default(); scheduler.Configure(settings, 0);
        Assert.True(scheduler.Trigger(HandGesture.Heart, 1, true));
        legacy.Playback = scheduler.Update(1.5); legacy.HandProfile = settings.Profiles.Single(p => p.Gesture == HandGesture.Heart);
        var active = runtime.Update(new(1.5, .5, 0)); Assert.Equal("Heart", runtime.State.ActionId);
        Assert.Contains(active.Layers, l => l.Id == "legacy-prop-Heart" && l.Opacity > 0);
        legacy.Playback = scheduler.Update(2.5); legacy.HandProfile = null;
        var ended = runtime.Update(new(2.5, 1, 0)); Assert.Null(runtime.State.ActionId); Assert.DoesNotContain(ended.Layers, l => l.Id.StartsWith("legacy-prop-"));
    }
    [Fact]
    public void SharedSchedulerCoalescesOlderCallerTimeAfterFrozenRuntimeFrame()
    {
        var runtime = new CharacterRuntime(new LegacySpriteSheetSource(3).Load());
        var scheduler = new HandMotionScheduler(runtime.Actions);
        scheduler.Configure(HandMotionSettings.Default(), 1);
        runtime.Update(new(100, 100, 0));
        // Windows frozen scene QA와 캐릭터/표정 변경의 이전 clock sample을 모두 수용한다.
        scheduler.Configure(HandMotionSettings.Default(), 2);
        Assert.True(scheduler.Trigger(HandGesture.Heart, 3, true));
        var playback = scheduler.Update(4); Assert.NotNull(playback); Assert.Equal(0, playback!.Progress);
        Assert.Equal(100, runtime.Actions.LastTime);
        Assert.Null(scheduler.Update(101.5));
    }
    [Fact]
    public void GarmentMeshMapsSourceVerticesAndKeepsFollowZeroAndSnapshots()
    {
        var legacy = new LegacySpriteSheetRuntime { Rig = CharacterRig.Default("neulbo") };
        var profile = new CostumeProfile(Guid.NewGuid(), "sleeve", CostumeCategory.Top, 50, 80, 230, 100, Attachment: MotionAttachment.Garment);
        legacy.Costumes = [new(profile, "sleeve.png"), new(profile with { Id = Guid.NewGuid(), Follow = 0 }, "fixed.png")];
        legacy.Playback = new(HandGesture.Heart, .5); legacy.HandProfile = HandMotionProfile.Default(HandGesture.Heart);
        var runtime = new CharacterRuntime(new LegacySpriteSheetSource(3).Load(), processor: legacy);
        var frame = runtime.Update(new(1, 1, 0)); var sleeve = frame.Layers.Single(l => l.Asset == "sleeve.png");
        Assert.NotNull(sleeve.Mesh); Assert.Null(frame.Layers.Single(l => l.Asset == "fixed.png").Mesh);
        Assert.Equal(50, sleeve.Matrix.OffsetX); Assert.Equal(230, sleeve.Width);
        var triangle = sleeve.Mesh!.Triangles.Last(); var expected = ConnectedMotion.Map(new(triangle.C.X, triangle.C.Y), legacy.Rig, legacy.Motion, 1, false);
        var actual = triangle.Matrix.Map(triangle.C.X, triangle.C.Y); Assert.Equal(expected.X, actual.X, 8); Assert.Equal(expected.Y, actual.Y, 8);
        var saved = sleeve.Mesh; legacy.Playback = null; legacy.HandProfile = null; runtime.Update(new(2, 1, 0));
        Assert.Same(saved, sleeve.Mesh); Assert.NotSame(frame, runtime.Frame);
    }
    [Theory]
    [InlineData(MotionAttachment.Head)]
    [InlineData(MotionAttachment.Mouth)]
    [InlineData(MotionAttachment.LeftHand)]
    [InlineData(MotionAttachment.RightHand)]
    public void AttachmentAffinePreservesWeightedPivotAndMouthOrder(MotionAttachment attachment)
    {
        var rig = CharacterRig.Default("neulbo") with { HeadMotion = true, MouthTravel = 20 };
        var pose = new MotionPose(12, 20, -9, 14, 7, -4, 1.2, HandGesture.Heart, 1);
        var identity = LegacyRenderGeometryProvider.Attachment(rig, pose, attachment, 0).Map(120, 100);
        Assert.Equal(120, identity.X, 8); Assert.Equal(100, identity.Y, 8);
        var actual = LegacyRenderGeometryProvider.Attachment(rig, pose, attachment, .5).Map(120, 100);
        Assert.True(double.IsFinite(actual.X) && double.IsFinite(actual.Y));
        if (attachment == MotionAttachment.Mouth)
        {
            var head = LegacyRenderGeometryProvider.Attachment(rig, pose, MotionAttachment.Head, .5).Map(120, 110);
            Assert.Equal(head.X, actual.X, 8); Assert.Equal(head.Y, actual.Y, 8);
        }
    }
}
