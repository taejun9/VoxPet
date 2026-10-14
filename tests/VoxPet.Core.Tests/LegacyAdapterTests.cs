using CharacterDefinition = VoxPet.Core.Character.CharacterDefinition;
using System.Text.Json;
using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class LegacyAdapterTests
{
    private static ImageSet Sample() => JsonSerializer.Deserialize<ImageSet>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ImageSetTemplate", "imageset.json")), ImageSet.JsonOptions)!;
    [Fact]
    public void BundledTemplatePreservesEveryAssetPartAndFrameBoundary()
    {
        var source = Sample(); var definition = LegacyImageSetAdapter.Convert(source);
        Assert.Equal(source.CanvasSize, definition.CanvasWidth); Assert.Equal(source.CanvasSize, definition.CanvasHeight);
        Assert.Equal(source.Files.Order(), definition.Assets.Order());
        foreach (var part in source.Parts)
        {
            var converted = definition.Parts.Single(p => p.Id == LegacyImageSetAdapter.PartId(part.Part));
            Assert.Equal(part.File, converted.Asset); Assert.Equal((int)part.Part * 10, converted.ZIndex);
        }
        Assert.Equal(source.Mouths, definition.Mouth!.Frames); Assert.Equal("legacy3", definition.Mouth.Curve);
        for (int i = 0; i < source.Mouths.Length; i++)
        {
            Assert.Equal(source.MouthOffsets![i].X, definition.Mouth.FrameTransforms[i].X);
            Assert.Equal(source.MouthOffsets[i].Y, definition.Mouth.FrameTransforms[i].Y);
        }
        foreach (var expression in source.Expressions)
        {
            var actual = definition.Expressions.Single(e => e.Id == expression.Kind.ToString().ToLowerInvariant());
            Assert.Equal(expression.EyesOpen, actual.EyesOpen); Assert.Equal(expression.EyesClosed, actual.EyesClosed); Assert.Equal(expression.Brows, actual.Brows);
        }
        for (int a = 0; a < source.Actions.Length; a++)
        {
            double time = 0; var original = source.Actions[a]; var action = definition.Actions[a];
            Assert.Equal(original.Name, action.Name); Assert.Equal(original.Template.ToString(), action.CompatibilityTag);
            Assert.Equal(original.Frames.Length, action.Timeline.Count);
            foreach (var (frame, index) in original.Frames.Select((f, i) => (f, i)))
            {
                var entry = action.Timeline[index]; Assert.Equal(time, entry.TimeMs); Assert.Equal(ActionEventKind.Pose, entry.Kind);
                var pose = definition.Poses.Single(p => p.Id == entry.Value); Assert.Equal(frame.Pose, pose.Name);
                Assert.Equal(frame.Replace.ToDictionary(p => LegacyImageSetAdapter.PartId(p.Key), p => p.Value).OrderBy(p => p.Key), pose.Sprites.OrderBy(p => p.Key));
                time += frame.DurationMs;
            }
            Assert.Equal(time, action.DurationMs);
        }
    }
    [Fact]
    public void ClothingAndWeaponPosesDependenciesAndOrderingAreRetained()
    {
        var source = Sample(); var definition = LegacyImageSetAdapter.Convert(source);
        var sleeve = definition.Attachments.Single(a => a.Id == "outfit-0").Layers.Single();
        Assert.Equal(source.Outfits[0].Layers[0].Poses.OrderBy(p => p.Key), sleeve.Poses.OrderBy(p => p.Key));
        Assert.Contains("arm-right", sleeve.RequiredVisibleParts); Assert.Equal((int)ImagePart.ArmR * 10 + 1, sleeve.ZIndex);
        var weapon = definition.Attachments.Single(a => a.Id == "weapon-0");
        Assert.Equal("Firearm", weapon.CompatibilityTag); Assert.Equal("legacyWeapon", weapon.ExclusiveGroup);
        foreach (var layer in source.Weapons[0].Layers)
        {
            var converted = weapon.Layers.Single(l => l.Id == LegacyImageSetAdapter.PartId(layer.Part));
            Assert.Equal(layer.Poses.OrderBy(p => p.Key), converted.Poses.OrderBy(p => p.Key));
            Assert.True(converted.HideOnMissingPose); Assert.Equal(layer.Part == ImagePart.Effect, converted.OnlyDuringAction);
        }
    }
    [Fact]
    public void HidingAndAllAnchorOffsetsSurviveWithoutChangingLegacyHierarchy()
    {
        var source = Sample(); var frame = source.Actions[0].Frames[0] with
        {
            Replace = new() { [ImagePart.Head] = null, [ImagePart.ArmR] = null },
            Anchors = new() { [ImageAnchor.Head] = new(2, 3), [ImageAnchor.Eye] = new(4, 5), [ImageAnchor.Face] = new(6, 7), [ImageAnchor.Mouth] = new(8, 9) }
        };
        var definition = LegacyImageSetAdapter.Convert(source with { Actions = [source.Actions[0] with { Frames = [frame] }] });
        var pose = definition.Poses.Single(); Assert.Null(pose.Sprites["head"]); Assert.Null(pose.Sprites["arm-right"]);
        foreach (var pair in frame.Anchors)
        {
            var transform = pose.Transforms[LegacyImageSetAdapter.AnchorId(pair.Key)];
            Assert.Equal(pair.Value.X, transform.X); Assert.Equal(pair.Value.Y, transform.Y);
        }
        Assert.Equal("head", definition.Anchors.Single(a => a.Id == "eyes").Parent);
        Assert.Equal("head", definition.Anchors.Single(a => a.Id == "mouth").Parent);
        Assert.Equal("head", definition.Anchors.Single(a => a.Id == "face").Parent);
    }
    [Fact]
    public void AdapterDoesNotRetainMutableLegacyArraysOrDictionaries()
    {
        var source = Sample(); ICharacterDefinitionSource sourceBoundary = new LegacyImageSetSource(source); var definition = sourceBoundary.Load();
        source.Parts[0] = new(ImagePart.Head, "changed.png"); source.Mouths[0] = "changed.png";
        source.Actions[0].Frames[0].Replace[ImagePart.ArmR] = "changed.png";
        source.Outfits[0].Layers[0].Poses["idle"] = "changed.png";
        Assert.DoesNotContain("changed.png", definition.Assets);
        Assert.Same(definition, sourceBoundary.Load());
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string?>)definition.Poses[0].Sprites).Clear());
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, string>)definition.Attachments[0].Layers[0].Poses).Clear());
    }
    [Fact]
    public void EightFramesAndMoustacheInheritanceAreExplicitCompatibilityData()
    {
        var source = Sample();
        var mustache = new OutfitImages("콧수염", [new(ImagePart.FaceDecoration, ImageAnchor.Mouth, new() { ["idle"] = "mustache.png" })]);
        var definition = LegacyImageSetAdapter.Convert(source with { Mouths = Enumerable.Range(0, 8).Select(i => $"mouth-{i}.png").ToArray(), MouthOffsets = null, Outfits = [mustache] });
        Assert.Equal("legacy8", definition.Mouth!.Curve); Assert.Equal(8, definition.Mouth.Frames.Count);
        var layer = definition.Attachments[0].Layers[0]; Assert.Equal("mouth", layer.Anchor); Assert.True(layer.Inherit.MouthMotion);
        Assert.Contains("head", layer.RequiredVisibleParts);
    }
    [Fact]
    public void InvalidOriginalAndBrokenNewReferencesAreRejected()
    {
        var source = Sample(); Assert.Throws<ArgumentException>(() => LegacyImageSetAdapter.Convert(source with { Version = 2 }));
        var body = new CharacterPart("body", "body", "body.png");
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([body], mouth: new(["mouth.png"])));
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([body], poses: [new("pose", sprites: new Dictionary<string, string?> { ["missing"] = null })]));
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([body], actions: [new("wave", "흔들기", 10, [new(0, ActionEventKind.Pose, value: "missing")])]));
        Assert.Throws<ArgumentException>(() => new CharacterDefinition([body], [new("root")], attachments: [new("hat", "모자", "hat", [new("hat", "hat.png", requiredVisibleParts: ["missing"])])]));
        Assert.Throws<ArgumentException>(() => new MouthDefinition([])); Assert.Throws<ArgumentException>(() => new MouthDefinition(Enumerable.Repeat("mouth.png", 17)));
        Assert.Throws<ArgumentException>(() => new MouthDefinition(["mouth.png"], curve: "legacy3"));
        Assert.Throws<ArgumentException>(() => new ActionDefinition("wave", "흔들기", 10, [new(11, ActionEventKind.Pose, value: "idle")]));
        Assert.Throws<ArgumentException>(() => new ExpressionDefinition("neutral", "../eyes.png", "closed.png"));
        Assert.Throws<ArgumentException>(() => new AttachmentDefinition("hat", "모자", "hat", []));
        Assert.Throws<ArgumentException>(() => new AttachmentLayer("hat", "hat.png", poses: new Dictionary<string, string> { ["idle"] = "../hat.png" }));
    }
}
