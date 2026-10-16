using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class CompanionSampleTests
{
    private static VoxPetPackage Load(string id) => new VoxPetPackageLoader().Load(Path.Combine(AppContext.BaseDirectory, "Samples", "companion-" + id + ".voxpet"));

    [Theory]
    [InlineData("neulbo", 8)]
    [InlineData("pelly", 3)]
    public void SamplesAugmentTheOriginalSheetInsteadOfAddingACharacter(string id, int mouths)
    {
        var package = Load(id); var definition = LegacySpriteSheetAdapter.Convert(mouths, samples: package.Character);
        Assert.Equal("voxpet.samples." + id, package.Manifest.Id);
        Assert.Single(definition.Parts); Assert.Equal("sprite", definition.Parts[0].Id);
        Assert.Equal(mouths, definition.SpriteSheet!.Selector.Frames.Count);
        Assert.DoesNotContain(package.AssetPaths, p => p.StartsWith("images/", StringComparison.Ordinal));
        Assert.DoesNotContain(package.AssetPaths, p => p.StartsWith("legacy/", StringComparison.Ordinal));
        Assert.Equal(17, definition.Attachments.Count); Assert.Equal(9, definition.Actions.Count);
        Assert.Equal(14, definition.Attachments.Select(a => a.Category).Distinct().Count());
        Assert.Equal(17, package.AssetPaths.Count(p => p.StartsWith("thumbnails/", StringComparison.Ordinal)));
        Assert.Equal(8, definition.Attachments.Single(a => a.Id == "hat").Layers[0].Animation!.Frames.Count);
        Assert.Equal(6, definition.Attachments.Single(a => a.Id == "cape").Layers[0].Animation!.Frames.Count);
        Assert.All(new[] { "hair-short", "hair-long", "hair-afro" }, hair =>
        {
            var layers = definition.Attachments.Single(a => a.Id == hair).Layers;
            Assert.Equal(2, layers.Count); Assert.True(layers[0].ZIndex < 1 && layers[1].ZIndex > 1);
            Assert.EndsWith("-fringe.png", layers[1].Asset);
        });
        using var buffer = new MemoryStream(new VoxPetPackageExporter().Export(package), false);
        var reloaded = new VoxPetPackageLoader().Load(buffer);
        Assert.All(package.AssetPaths, p => Assert.Equal(package.ReadAsset(p), reloaded.ReadAsset(p)));
    }

    public static IEnumerable<object[]> Actions() =>
        from character in new[] { "neulbo", "pelly" }
        from action in new[] { "claw", "toy-gun", "heart", "toy-sword", "magic-wand", "firearm", "bow", "one-hand", "two-hand" }
        select new object[] { character, action };

    [Theory]
    [MemberData(nameof(Actions))]
    public void AllActionsKeepTheOriginalFaceAndShareTheExistingHandEngine(string id, string action)
    {
        var package = Load(id); var definition = LegacySpriteSheetAdapter.Convert(id == "neulbo" ? 8 : 3, samples: package.Character);
        var processor = new LegacySpriteSheetRuntime { Samples = package.Character, Rig = CharacterRig.Default(id), PreserveForegroundWing = id == "pelly" };
        processor.Apply(ExpressionProfile.Default(0), id == "neulbo" ? 8 : 3, 0);
        var runtime = new CharacterRuntime(definition, processor: processor, geometry: processor);
        var hands = new HandMotionScheduler(runtime.Actions); hands.Configure(HandMotionSettings.Default(), 0);
        runtime.Attachments.SetEnabled("top", true); runtime.Attachments.SetEnabled("hat", true); runtime.Attachments.SetEnabled("cape", true);
        runtime.RequestAction(action); runtime.Update(new(0, 0, 0, false));
        Assert.Null(hands.Update(.2)); // A sample ID is not a HandGesture enum string.
        var frame = runtime.Update(new(.2, .2, 0, false));
        Assert.Equal(action, runtime.State.ActionId); Assert.NotNull(processor.Motion.Gesture);
        Assert.Contains(frame.Layers, l => l.Id == "legacy-sprite" && l.Asset == LegacySpriteSheetAdapter.Asset(0, 1) && l.Mesh != null);
        Assert.DoesNotContain(frame.Layers, l => l.Id.StartsWith("part-", StringComparison.Ordinal) || l.Id.StartsWith("face-", StringComparison.Ordinal));
        Assert.Contains(frame.Layers, l => l.Asset == "samples/top.png" && l.Mesh != null);
        Assert.Contains(frame.Layers, l => l.Asset.StartsWith("samples/hat", StringComparison.Ordinal));
        Assert.Contains(frame.Layers, l => l.Asset.StartsWith("samples/cape", StringComparison.Ordinal));
        Assert.Equal(id == "pelly", frame.Layers.Any(l => l.Id == "legacy-sample-foreground-wing"));
        processor.Parameters = new(1, 0, 0, 0, 0, MouthState.Open);
        processor.Apply(ExpressionProfile.Default(0) with { Blink = true }, id == "neulbo" ? 8 : 3, .2);
        var talking = runtime.Update(new(.4, .2, 1, true));
        Assert.Contains(talking.Layers, l => l.Id == "legacy-sprite" && l.Asset == LegacySpriteSheetAdapter.Asset(id == "neulbo" ? 7 : 2, 0));
        runtime.Update(new(1.3, .9, 0, false)); Assert.Null(runtime.State.ActionId);
        Assert.Null(processor.Motion.Gesture);
        Assert.Equal(new[] { "top", "hat", "cape" }, runtime.Attachments.EnabledIds);
        Assert.True(hands.Trigger(HandGesture.Heart, 1.4, true));
        Assert.Equal(HandGesture.Heart, hands.Update(1.6)!.Gesture);
    }

    [Fact]
    public void ProfilesUseDifferentRegisteredArtAndKeepHairAndHatCompatible()
    {
        var front = Load("neulbo"); var side = Load("pelly");
        Assert.NotEqual(front.ReadAsset("samples/top.png"), side.ReadAsset("samples/top.png"));
        foreach (var package in new[] { front, side })
        {
            var attachments = new VoxPet.Core.Attachments.AttachmentSystem(package.Character.Attachments);
            attachments.SetEnabled("hat", true); attachments.SetEnabled("cape", true);
            foreach (string hair in new[] { "hair-short", "hair-long", "hair-afro" })
            {
                attachments.SetEnabled(hair, true);
                Assert.Equal(3, attachments.EnabledIds.Count); Assert.Contains("hat", attachments.EnabledIds); Assert.Contains("cape", attachments.EnabledIds); Assert.Contains(hair, attachments.EnabledIds);
            }
            attachments.SetEnabled("headAccessory", true);
            Assert.DoesNotContain(attachments.EnabledIds, id => id.StartsWith("hair-", StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void NineTimelinesHaveNineDistinctArmPoses(string id)
    {
        var package = Load(id); var definition = LegacySpriteSheetAdapter.Convert(id == "neulbo" ? 8 : 3, samples: package.Character);
        var poses = new HashSet<(double Left, double Right)>();
        foreach (var action in package.Character.Actions)
        {
            var processor = new LegacySpriteSheetRuntime { Samples = package.Character, Rig = CharacterRig.Default(id) };
            var runtime = new CharacterRuntime(definition, processor: processor, geometry: processor);
            runtime.RequestAction(action.Id); runtime.Update(new(0, 0, 0)); runtime.Update(new(.2, .2, 0));
            poses.Add((processor.Motion.LeftAngle, processor.Motion.RightAngle));
            if (action.Id == "one-hand") Assert.Equal(0, processor.Motion.LeftAngle);
            if (action.Id == "two-hand" && id == "neulbo") Assert.True(processor.Motion.LeftAngle < 0 && processor.Motion.RightAngle > 0);
            if (id == "pelly") Assert.Equal(0, processor.Motion.LeftAngle);
        }
        Assert.Equal(9, poses.Count);
    }
}
