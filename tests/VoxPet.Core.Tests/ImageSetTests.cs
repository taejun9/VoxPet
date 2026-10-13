using System.Text.Json;
using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class ImageSetTests
{
    private static ImageSet Sample() => JsonSerializer.Deserialize<ImageSet>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ImageSetTemplate", "imageset.json")), ImageSet.JsonOptions)!;
    [Fact]
    public void FrameReplacesArmAndSleeveWithoutAddingAnotherArm()
    {
        var player = new ImageSetPlayer(Sample()); Assert.True(player.Play(0, 10));
        var ready = player.Compose(10, 0, false, ExpressionKind.Neutral, 0, 0);
        var right = ready.Where(i => i.Part == ImagePart.ArmR).ToArray();
        Assert.Equal(2, right.Length); Assert.Equal("arm-ready.png", right[0].File); Assert.Equal("sleeve-ready.png", right[1].File);
        Assert.DoesNotContain(ready, i => i.File == "arm-r.png" || i.File == "sleeve-idle.png");
        Assert.Contains(ready, i => i.File == "gun-ready.png"); Assert.DoesNotContain(ready, i => i.Part == ImagePart.Effect);
        var fire = player.Compose(10.1, 1, true, ExpressionKind.Happy, 0, 0);
        Assert.Contains(fire, i => i.File == "effect-fire.png"); Assert.Contains(fire, i => i.File == "mouth-2.png"); Assert.Contains(fire, i => i.File == "eyes-closed.png");
        Assert.Equal(fire.OrderBy(i => i.Part).ThenBy(i => i.Costume), fire);
    }
    [Fact]
    public void RapidClicksRestartImmediatelyAndCompletionRestoresIdle()
    {
        var player = new ImageSetPlayer(Sample()); player.Play(0, 0);
        Assert.Equal("fire", player.Frame(.12)!.Pose);
        player.Play(0, .12); Assert.Equal("ready", player.Frame(.12)!.Pose);
        Assert.Null(player.Frame(.5));
        Assert.Contains(player.Compose(.5, 0, false, ExpressionKind.Neutral, 0, 0), i => i.File == "arm-r.png");
        player.Play(0, 1); player.Stop(); Assert.Null(player.Frame(1));
        Assert.False(player.Play(-1, 0)); Assert.False(player.Play(0, double.NaN));
    }
    [Fact]
    public void NullOverridesHideBaseClothingAndFace()
    {
        var sample = Sample(); var frame = sample.Actions[0].Frames[0] with { Replace = new() { [ImagePart.ArmR] = null, [ImagePart.Head] = null } };
        var player = new ImageSetPlayer(sample with { Actions = [sample.Actions[0] with { Frames = [frame] }] }); player.Play(0, 0);
        var images = player.Compose(0, 1, false, ExpressionKind.Neutral, 0);
        Assert.DoesNotContain(images, i => i.Part is ImagePart.ArmR or ImagePart.Head or ImagePart.Eyes or ImagePart.Mouth or ImagePart.Brows);
        Assert.Contains(images, i => i.Part == ImagePart.ArmL);
    }
    [Fact]
    public void FacialDecorationTracksMouthWhileGlassesTrackHead()
    {
        var sample = Sample(); var frame = sample.Actions[0].Frames[0] with { Anchors = new() { [ImageAnchor.Head] = new(2, 3), [ImageAnchor.Mouth] = new(1, 2) } };
        var mustache = new OutfitImages("콧수염", [new(ImagePart.FaceDecoration, ImageAnchor.Mouth, new() { ["idle"] = "decoration.png" })]);
        var glasses = new OutfitImages("안경", [new(ImagePart.FaceDecoration, ImageAnchor.Head, new() { ["idle"] = "glasses.png" })]);
        var player = new ImageSetPlayer(sample with { Outfits = [mustache, glasses], Actions = [sample.Actions[0] with { Frames = [frame] }] }); player.Play(0, 0);
        Assert.Equal(new AnchorOffset(3, 10), player.Compose(0, 1, false, ExpressionKind.Neutral, 0).Single(i => i.Part == ImagePart.FaceDecoration).Offset);
        Assert.Equal(new AnchorOffset(2, 3), player.Compose(0, 1, false, ExpressionKind.Neutral, 1).Single(i => i.Part == ImagePart.FaceDecoration).Offset);
    }
    [Theory]
    [InlineData("../arm.png")]
    [InlineData("C:/arm.png")]
    [InlineData("https://host/arm.png")]
    [InlineData("/arm.png")]
    [InlineData("arms\\arm.png")]
    [InlineData("arms//arm.png")]
    [InlineData("arm.jpg")]
    [InlineData(null)]
    public void AssetPathsStayInsideTheSet(string? file) => Assert.Throws<ArgumentException>(() => ImageSet.ValidateFile(file));
    [Fact]
    public void MissingPartsSleevePosesWeaponPosesAndInvalidNumbersAreRejected()
    {
        var sample = Sample();
        Assert.Throws<ArgumentException>(() => (sample with { Parts = sample.Parts[1..] }).Validate());
        Assert.Throws<ArgumentException>(() => (sample with { Parts = [.. sample.Parts, sample.Parts[0]] }).Validate());
        Assert.Throws<ArgumentException>(() => (sample with { CanvasSize = 513 }).Validate());
        Assert.Throws<ArgumentException>(() => (sample with { CharacterId = "neulbo-girl" }).Validate());
        Assert.Throws<ArgumentException>(() => (sample with { MouthOffsets = [new(double.NaN, 0), new(), new()] }).Validate());
        var outfit = sample.Outfits[0] with { Layers = [sample.Outfits[0].Layers[0] with { Poses = new() { ["idle"] = "idle.png" } }] };
        Assert.Throws<ArgumentException>(() => (sample with { Outfits = [outfit] }).Validate());
        var weapon = sample.Weapons[0] with { Layers = [sample.Weapons[0].Layers[0] with { Poses = new() { ["idle"] = "idle.png" } }] };
        Assert.Throws<ArgumentException>(() => (sample with { Weapons = [weapon] }).Validate());
        Assert.Throws<ArgumentException>(() => (sample with { Expressions = [new(ExpressionKind.Neutral, null!, "eyes.png")] }).Validate());
        var frame = sample.Actions[0].Frames[0] with { DurationMs = 0 };
        Assert.Throws<ArgumentException>(() => (sample with { Actions = [sample.Actions[0] with { Frames = [frame] }] }).Validate());
    }
    [Fact]
    public void IncompatibleWeaponIsNotDrawnAndMissingExpressionFallsBack()
    {
        var sample = Sample(); var player = new ImageSetPlayer(sample with { Weapons = [sample.Weapons[0] with { Template = ActionTemplate.Bow }] });
        player.Play(0, 0); var images = player.Compose(0, double.NaN, false, ExpressionKind.Angry, weapon: 0);
        Assert.DoesNotContain(images, i => i.Part == ImagePart.Weapon); Assert.Contains(images, i => i.File == "eyes-open.png"); Assert.Contains(images, i => i.File == "mouth-0.png");
    }
    [Fact]
    public void PreferencesPersistBindingsAndRecoverBadSelectionWithoutTouchingOriginal()
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-image-test-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, "runtime.json"); var store = new RuntimePreferencesStore(path);
            var value = new RuntimePreferences(new() { ["pelly"] = new(new string('a', 32), 2, 1, 0) }, [new(RuntimeOperation.Play, 0x51, 0)], true);
            Assert.True(store.Save(value)); var saved = store.Load(); Assert.Equal(value.Characters["pelly"], saved.Characters["pelly"]); Assert.Equal(value.Bindings, saved.Bindings); Assert.True(saved.LeftClick);
            Assert.Throws<ArgumentException>(() => (value with { Bindings = [new(RuntimeOperation.Play, 0x70, 6)] }).Validate());
            Assert.Throws<ArgumentException>(() => (value with { Bindings = [new(RuntimeOperation.Play, 0x51, 0), new(RuntimeOperation.Reset, 0x51, 0)] }).Validate());
            File.WriteAllText(path, "broken"); Assert.Empty(store.Load().Characters); Assert.Equal("broken", File.ReadAllText(path));
        }
        finally { Directory.Delete(folder, true); }
    }
}
