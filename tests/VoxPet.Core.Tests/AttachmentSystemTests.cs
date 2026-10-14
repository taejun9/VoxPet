using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Motion;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class AttachmentSystemTests
{
    private static AttachmentDefinition Accessory(string id, string category, string anchor = "root", AttachmentInheritance? inherit = null,
        AttachmentTransform? placement = null) => new(id, id, category, [new("image", id + ".png", anchor, inherit: inherit, placement: placement)]);
    private static RigDefinition Face() => new([new("root"), new("head", 100, 100, "root"), new("mouth", 0, 20, "head"), new("tail", 40, 0, "root")]);
    [Fact]
    public void MoustacheFollowsMouthAndHeadWhileGlassesExcludeMouthMotion()
    {
        var solver = new RigMotionSolver(Face());
        var pose = solver.Resolve(new Dictionary<string, Transform2D> { ["head"] = new(2, 3, rotation: 90) }, "mouth", new(0, 10));
        var system = new AttachmentSystem([Accessory("mustache", "faceDecoration", "mouth", new(MouthMotion: true)), Accessory("glasses", "glasses", "mouth")]);
        system.SetEnabled("mustache", true); system.SetEnabled("glasses", true);
        var layers = system.Resolve(pose);
        var mustache = layers.Single(l => l.Asset == "mustache.png").Matrix.Map(100, 120);
        var glasses = layers.Single(l => l.Asset == "glasses.png").Matrix.Map(100, 120);
        Assert.Equal(72, mustache.X, 10); Assert.Equal(103, mustache.Y, 10);
        Assert.Equal(82, glasses.X, 10); Assert.Equal(103, glasses.Y, 10);
    }
    [Fact]
    public void InheritanceComponentsAndMouthMotionAreIndependent()
    {
        var solver = new RigMotionSolver(Face()); var pose = solver.Resolve(new Dictionary<string, Transform2D> { ["head"] = new(10, 20, 2, 2, 90) }, "mouth", new(0, 5));
        var system = new AttachmentSystem([Accessory("fixed", "effect", "head", new(false, false, false)),
            Accessory("position", "effect", "head", new(true, false, false)), Accessory("rotation", "effect", "head", new(false, true, false)),
            Accessory("scale", "effect", "head", new(false, false, true)), Accessory("mouth-only", "effect", "mouth", new(false, false, false, true))]);
        foreach (string id in new[] { "fixed", "position", "rotation", "scale", "mouth-only" }) system.SetEnabled(id, true);
        var layers = system.Resolve(pose).ToDictionary(l => l.Asset);
        Assert.Equal(new RigPoint(105, 100), layers["fixed.png"].Matrix.Map(105, 100));
        Assert.Equal(new RigPoint(115, 120), layers["position.png"].Matrix.Map(105, 100));
        var rotated = layers["rotation.png"].Matrix.Map(105, 100); Assert.Equal(100, rotated.X, 10); Assert.Equal(105, rotated.Y, 10);
        Assert.Equal(new RigPoint(110, 100), layers["scale.png"].Matrix.Map(105, 100));
        var mouth = layers["mouth-only.png"].Matrix.Map(100, 120); Assert.Equal(100, mouth.X, 10); Assert.Equal(125, mouth.Y, 10);
    }
    [Fact]
    public void PartialAndZeroFollowKeepLegacyRotationAndMouthOrder()
    {
        var solver = new RigMotionSolver(Face()); var pose = solver.Resolve(new Dictionary<string, Transform2D> { ["head"] = new(rotation: 40) }, "mouth", new(0, 10));
        var system = new AttachmentSystem([Accessory("partial", "effect", "mouth", new(MouthMotion: true), new(follow: .5)),
            Accessory("fixed", "effect", "mouth", new(MouthMotion: true), new(follow: 0))]);
        system.SetEnabled("partial", true); system.SetEnabled("fixed", true); var layers = system.Resolve(pose);
        Assert.Equal(new RigPoint(100, 120), layers.Single(l => l.Asset == "fixed.png").Matrix.Map(100, 120));
        // 부분 position 상속은 anchor 중심 이동의 선형 interpolation이다. extra mouth는 필터된 bone 회전 뒤 적용한다.
        var partial = layers.Single(l => l.Asset == "partial.png").Matrix;
        var without = new AttachmentSystem([Accessory("base", "effect", "mouth", placement: new(follow: .5))]); without.SetEnabled("base", true);
        var baseline = without.Resolve(pose).Single().Matrix.Map(100, 120); var moved = partial.Map(100, 120);
        Assert.Equal(-Math.Sin(20 * Math.PI / 180) * 5, moved.X - baseline.X, 10);
        Assert.Equal(Math.Cos(20 * Math.PI / 180) * 5, moved.Y - baseline.Y, 10);
    }
    [Fact]
    public void ConflictsRestoreAndCustomAnchorsPreserveSelectionRules()
    {
        var system = new AttachmentSystem([Accessory("top", "top"), Accessory("bottom", "bottom"), Accessory("dress", "onePiece"),
            Accessory("coat", "outerwear"), Accessory("tail", "effect", "tail"), Accessory("dec1", "faceDecoration"), Accessory("dec2", "faceDecoration")]);
        system.SetEnabled("top", true); system.SetEnabled("bottom", true); system.SetEnabled("coat", true); system.SetEnabled("dress", true);
        Assert.Equal(new[] { "coat", "dress" }, system.EnabledIds); system.SetEnabled("bottom", true); Assert.DoesNotContain("dress", system.EnabledIds);
        system.SetEnabled("dec1", true); system.SetEnabled("dec2", true); Assert.Contains("dec1", system.EnabledIds); Assert.Contains("dec2", system.EnabledIds);
        system.Restore(["top", "dress", "bottom"]); Assert.Equal(new[] { "top", "bottom" }, system.EnabledIds);
        system.SetEnabled("tail", true); var pose = new RigMotionSolver(Face()).Resolve(new Dictionary<string, Transform2D> { ["tail"] = new(7, 8) });
        Assert.Equal(new RigPoint(7, 8), system.Resolve(pose).Single(l => l.Asset == "tail.png").Matrix.Map(0, 0));
        Assert.Equal(14, RigDefinition.Standard().Anchors.Count); Assert.Equal(14, AttachmentSystem.Categories.Count);
    }
    [Fact]
    public void NonuniformParentRotationAndScalePreserveExactAffineInheritance()
    {
        var rig = new RigDefinition([new("root", transform: new(10, 20, 2, 3, 30)), new("custom", 5, 7, "root")]);
        var solver = new RigMotionSolver(rig); var pose = solver.Resolve(new Dictionary<string, Transform2D> { ["custom"] = new(2, 3, 1.5, .8, 40) });
        var system = new AttachmentSystem([Accessory("tail", "effect", "custom")]); system.SetEnabled("tail", true);
        var baselinePoint = pose.Static["custom"].Map(4, 8); var expected = pose.Animated["custom"].Map(4, 8);
        var actual = system.Resolve(pose).Single().Matrix.Map(baselinePoint.X, baselinePoint.Y);
        Assert.Equal(expected.X, actual.X, 10); Assert.Equal(expected.Y, actual.Y, 10);
    }
    [Fact]
    public void LayerPosesHiddenDependenciesAndWeaponCompatibilityStayInCore()
    {
        var weapon = new AttachmentDefinition("weapon", "무기", "handAccessory", [new("gun", "idle.png", poses: new Dictionary<string, string> { ["idle"] = "idle.png", ["fire"] = "fire.png" }, hideOnMissingPose: true),
            new("effect", "effect.png", onlyDuringAction: true)], "Firearm");
        var sleeve = new AttachmentDefinition("sleeve", "소매", "top", [new("sleeve", "sleeve.png", requiredVisibleParts: ["arm"])]);
        var system = new AttachmentSystem([weapon, sleeve]); system.SetEnabled("weapon", true); system.SetEnabled("sleeve", true);
        var pose = new RigMotionSolver(new([new("root")])).Resolve();
        var engine = new ActionEngine([new("fire", "fire")]); engine.TryPlay(new("gun", "발사", 1000, [new(0, ActionEventKind.Pose, value: "fire")]), 0);
        var layers = system.Compose(pose, engine.Current, "Firearm", new HashSet<string>());
        Assert.Contains(layers, l => l.Asset == "fire.png"); Assert.Contains(layers, l => l.Asset == "effect.png"); Assert.DoesNotContain(layers, l => l.Asset == "sleeve.png");
        Assert.Empty(system.Compose(pose, engine.Current, "Other", new HashSet<string>()));
        Assert.DoesNotContain(system.Resolve(pose), l => l.Asset == "effect.png");
    }
    [Fact]
    public void ReflectionFollowNeverCreatesAZeroScaleAndTinyEndpointIsPreserved()
    {
        var solver = new RigMotionSolver(new([new("root")]));
        var reflected = solver.Resolve(new Dictionary<string, Transform2D> { ["root"] = new(scaleX: -1) });
        Assert.True(double.IsFinite(reflected.Bone(.5)["root"].Inverse().M11));
        var tiny = solver.Resolve(new Dictionary<string, Transform2D> { ["root"] = new(scaleX: 1e-200, scaleY: 1e-200) });
        Assert.Equal(1e-200, tiny.Bone(1)["root"].M11);
    }
    [Fact]
    public void InvalidRigCategoriesReferencesAndDuplicateIdsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new RigDefinition([new("a", parent: "b"), new("b", parent: "a")]));
        Assert.Throws<ArgumentException>(() => new RigDefinition([new("a", parent: "missing")]));
        Assert.Throws<ArgumentException>(() => new RigDefinition([new("a"), new("a")]));
        Assert.Throws<ArgumentException>(() => new AttachmentSystem([Accessory("bad", "unknown")]));
        var system = new AttachmentSystem([Accessory("hat", "hat")]);
        Assert.Throws<ArgumentException>(() => system.SetEnabled("missing", true)); Assert.Throws<ArgumentException>(() => system.Restore(["missing"]));
        var solver = new RigMotionSolver(Face()); Assert.Throws<ArgumentException>(() => solver.Resolve(new Dictionary<string, Transform2D> { ["missing"] = new() }));
    }
}
