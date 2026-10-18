using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using Xunit;
using CharacterDefinition = VoxPet.Core.Character.CharacterDefinition;

namespace VoxPet.Core.Tests;

public sealed class NaturalCompanionTests
{
    private static VoxPetPackage Load(string id) => new VoxPetPackageLoader().Load(Path.Combine(AppContext.BaseDirectory, "Samples", "companion-" + id + ".voxpet"));
    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 50)]
    public void TransformInterpolationIsOptInAndContinuous(bool interpolate, double middle)
    {
        var action = new ActionDefinition("move", "Move", 1000, [new(0, ActionEventKind.Transform, "hand", transform: new()), new(1000, ActionEventKind.Transform, "hand", transform: new(x: 100, rotation: 80))], interpolateTransforms: interpolate);
        var runtime = new CharacterRuntime(new([new("body", "body", "body.png")], [new("root"), new("hand", parent: "root")], actions: [action]));
        runtime.RequestAction("move"); runtime.Update(new(0, 0, 0)); runtime.Update(new(.5, .5, 0));
        Assert.Equal(middle, runtime.CurrentRig.Animated["hand"].OffsetX, 8);
        var before = runtime.Update(new(.501, .001, 0)); Assert.NotNull(before);
        Assert.InRange(runtime.CurrentRig.Animated["hand"].OffsetX - middle, 0, interpolate ? .2 : 0);
    }
    [Fact]
    public void HiddenOrIncompatibleReplacementNeverRemovesTheOriginalFoot()
    {
        var shoe = new AttachmentDefinition("shoe", "Shoe", "shoes", [new("front", "shoe.png", requiredVisibleParts: ["foot"], replacesParts: ["foot"], onlyDuringAction: true)]);
        var definition = new CharacterDefinition([new("foot", "leg", "foot.png")], [new("root")], attachments: [shoe], actions: [new("step", "Step", 1000, [])]);
        var runtime = new CharacterRuntime(definition); runtime.Attachments.SetEnabled("shoe", true);
        Assert.Contains(runtime.Update(new(0, 0, 0)).Layers, l => l.Id == "part-foot");
        runtime.RequestAction("step"); var frame = runtime.Update(new(.1, .1, 0));
        Assert.DoesNotContain(frame.Layers, l => l.Id == "part-foot"); Assert.Contains(frame.Layers, l => l.Asset == "shoe.png");
        runtime.StopActions(); Assert.Contains(runtime.Update(new(.2, .1, 0)).Layers, l => l.Id == "part-foot");
        Assert.Throws<ArgumentException>(() => new CharacterDefinition(definition.Parts, definition.Anchors, attachments: [new("bad", "Bad", "shoes", [new("x", "shoe.png", replacesParts: ["missing"])])]));
    }
    [Fact]
    public void HeadReplacementAlsoHidesTheFaceThatBelongsToIt()
    {
        var helmet = new AttachmentDefinition("helmet", "Helmet", "hat", [new("front", "helmet.png", requiredVisibleParts: ["head"], replacesParts: ["head"])]);
        var runtime = new CharacterRuntime(new([new("head", "head", "head.png", "head")], [new("root"), new("head", parent: "root")], expressions: [new("neutral", "eyes.png", "closed.png")], attachments: [helmet]));
        runtime.Attachments.SetEnabled("helmet", true); var frame = runtime.Update(new(0, 0, 0));
        Assert.Single(frame.Layers); Assert.Equal("helmet.png", frame.Layers[0].Asset);
        runtime.Attachments.SetEnabled("helmet", false); frame = runtime.Update(new(.1, .1, 0));
        Assert.Contains(frame.Layers, l => l.Id == "part-head"); Assert.Contains(frame.Layers, l => l.Id == "face-eyes");
    }
    [Fact]
    public void ZeroFollowStillDisablesTheNewWristSkinning()
    {
        var anchors = new AnchorDefinition[] { new("root"), new("body", parent: "root"), new("shoulder", 300, 300, "body"), new("hand", 50, 120, "shoulder") };
        var sleeve = new AttachmentDefinition("sleeve", "Sleeve", "top", [new("cloth", "cloth.png", "shoulder", placement: new(follow: 0, skinning: new(300, 420, "hand", "body")))]);
        var action = new ActionDefinition("move", "Move", 1000, [new(0, ActionEventKind.Transform, "hand", transform: new(x: -80, y: -40))]);
        var runtime = new CharacterRuntime(new([new("arm", "arm", "arm.png", "shoulder", skinning: new(300, 420, "hand", "body"))], anchors, actions: [action], attachments: [sleeve]));
        runtime.Attachments.SetEnabled("sleeve", true); runtime.RequestAction("move"); var frame = runtime.Update(new(0, 0, 0));
        Assert.NotNull(frame.Layers.Single(l => l.Id == "part-arm").Mesh);
        var garment = frame.Layers.Single(l => l.Asset == "cloth.png"); Assert.Null(garment.Mesh); Assert.Equal(AffineTransform2D.Identity, garment.Matrix);
    }
    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void BothHandsStayOnTheLongHiltAndTheBowStringFollowsTheDrawingFist(string id)
    {
        var p = Load(id); var runtime = p.CreateRuntime();
        foreach (string action in new[] { "two-hand", "bow" })
        {
            runtime.StopActions(); runtime.RequestAction(action); double start = runtime.Actions.LastTime;
            runtime.Update(new(start, 0, 0));
            foreach (double t in new[] { .26, .42, .59, .70 })
            {
                var frame = runtime.Update(new(start + t, .01, 0)); var rig = runtime.CurrentRig.Animated;
                var right = rig["rightHand"].Map(0, 0); var left = rig["leftHand"].Map(0, 0);
                if (action == "two-hand")
                {
                    var support = rig["rightHand"].Map(0, 23);
                    Assert.Equal(support.X, left.X, 3); Assert.Equal(support.Y, left.Y, 3);
                }
                else
                {
                    Assert.True(right.X > left.X + 20);
                    var strings = frame.Layers.Where(l => l.Asset == "samples/bow-string.png").ToArray(); Assert.Equal(2, strings.Length);
                    foreach (var line in strings)
                    { var end = line.Matrix.Map(line.Width, 0); Assert.Equal(right.X, end.X, 6); Assert.Equal(right.Y, end.Y, 6); }
                }
            }
        }
        var decoded = new VoxPetPackageLoader().Load(new MemoryStream(new VoxPetPackageExporter().Export(p)));
        Assert.All(decoded.Character.Actions, a => Assert.True(a.InterpolateTransforms));
        Assert.All(decoded.Character.Attachments.Single(a => a.Id == "shoes").Layers, l => Assert.Equal(l.RequiredVisibleParts, l.ReplacesParts));
        Assert.Equal("rightHand", decoded.Character.Parts.Single(a => a.Id == "arm-right").Skinning!.EndAnchor);
        Assert.Equal(2, decoded.Character.Actions.Single(a => a.Id == "bow").Timeline.Count(e => e.EndAnchor == "rightHand" && e.Value == "samples/bow-string.png"));
    }
    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void MovingTorsoAndWristKeepTheMeshRootAndEndOnTheirOwnAnchors(string id)
    {
        var p = Load(id); var runtime = p.CreateRuntime(); runtime.RequestAction("toy-gun"); runtime.Update(new(0, 0, 0));
        var frame = runtime.Update(new(.405, .405, 0)); var skin = p.Character.Parts.Single(p => p.Id == "arm-right").Skinning!;
        var root = runtime.CurrentRig.Animated[skin.RootAnchor!].Compose(runtime.CurrentRig.Static[skin.RootAnchor!].Inverse());
        foreach (var triangle in frame.Layers.Single(l => l.Id == "part-arm-right").Mesh!.Triangles)
            foreach (var point in new[] { triangle.A, triangle.B, triangle.C })
            {
                var actual = triangle.Matrix.Map(point.X, point.Y);
                if (point.Y <= skin.RootY)
                {
                    var expected = root.Map(point.X, point.Y);
                    Assert.Equal(expected.X, actual.X, 7); Assert.Equal(expected.Y, actual.Y, 7);
                }
            }
        var bind = runtime.CurrentRig.Static["rightHand"].Map(0, 0);
        var edge = frame.Layers.Single(l => l.Id == "part-arm-right").Mesh!.Triangles.First(t => t.A.Y == skin.FullY && t.B.Y == skin.FullY && t.A.X <= bind.X && t.B.X >= bind.X);
        var wrist = edge.Matrix.Map(bind.X, bind.Y); var target = runtime.CurrentRig.Animated["rightHand"].Map(0, 0);
        Assert.Equal(target.X, wrist.X, 7); Assert.Equal(target.Y, wrist.Y, 7);
    }
    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void WristbandFollowsTheForearmInsteadOfFloatingBesideTheRotatingFist(string id)
    {
        var runtime = Load(id).CreateRuntime(); runtime.Attachments.SetEnabled("handAccessory", true); runtime.RequestAction("firearm"); runtime.Update(new(0, 0, 0));
        var frame = runtime.Update(new(.405, .405, 0));
        Assert.Same(frame.Layers.Single(l => l.Id == "part-arm-right").Mesh, frame.Layers.Single(l => l.Asset == "samples/handAccessory.png").Mesh);
    }
    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void ExpressionCrossfadeKeepsPosedBodyPartsOpaqueAndMovesBothHeadsTogether(string id)
    {
        var p = Load(id); var processor = new LegacySpriteSheetRuntime { Samples = p.Character, Rig = CharacterRig.Default(id), WeaponId = "firearm", HoldWeapon = false };
        var runtime = new CharacterRuntime(LegacySpriteSheetAdapter.Convert(3, samples: p.Character), processor: processor);
        runtime.RequestAction("firearm"); runtime.Update(new(0, 0, 0)); runtime.Update(new(.38, .38, 0));
        processor.Apply(ExpressionProfile.Default(1), 3, .38); var frame = runtime.Update(new(.38, 0, 0));
        Assert.DoesNotContain(frame.Layers, l => l.Id == "legacy-previous");
        Assert.All(frame.Layers.Where(l => l.Id.StartsWith("part-", StringComparison.Ordinal) && l.Id != "part-head"), l => Assert.Equal(1, l.Opacity));
        Assert.Equal(frame.Layers.Single(l => l.Id == "part-head").Matrix, frame.Layers.Single(l => l.Id == "legacy-previous-head").Matrix);
        Assert.Contains(frame.Layers, l => l.Id == "companion-equipped-weapon");
    }
    [Theory]
    [InlineData("claw")]
    [InlineData("heart")]
    [InlineData("toy-gun")]
    [InlineData("toy-sword")]
    [InlineData("magic-wand")]
    [InlineData("firearm")]
    [InlineData("bow")]
    [InlineData("one-hand")]
    [InlineData("two-hand")]
    public void FoldingWingNeverTurnsItsFeatherStripsInsideOut(string action)
    {
        var runtime = Load("pelly").CreateRuntime(); runtime.RequestAction(action); runtime.Update(new(0, 0, 0));
        for (int i = 1; i < 60; i++)
        {
            var frame = runtime.Update(new(i * .02, .02, 0));
            var mesh = frame.Layers.Single(l => l.Id == "part-arm-right").Mesh;
            if (mesh == null) continue;
            foreach (var triangle in mesh.Triangles)
            {
                double x = (triangle.A.X + triangle.B.X + triangle.C.X) / 3, y = (triangle.A.Y + triangle.B.Y + triangle.C.Y) / 3;
                if (x is <= 300 or >= 440 || y is <= 302 or >= 402) continue;
                var m = triangle.Matrix;
                Assert.True(m.M11 * m.M22 - m.M12 * m.M21 > 0, $"{action} at {i * 20}ms folds a feather strip");
            }
        }
    }
    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void ShootingHasBriefMuzzleFlashRecoilAndRecovery(string id)
    {
        var p = Load(id); var runtime = p.CreateRuntime(); runtime.RequestAction("firearm"); runtime.Update(new(0, 0, 0));
        var before = runtime.Update(new(.35, .35, 0)); var aim = runtime.CurrentRig.Animated["rightHand"];
        var aimLocal = runtime.CurrentRig.Animated["body"].Inverse().Compose(aim);
        var shot = runtime.Update(new(.38, .03, 0));
        Assert.DoesNotContain(before.Layers, l => l.Asset == "samples/muzzle-flash.png"); Assert.Contains(shot.Layers, l => l.Asset == "samples/muzzle-flash.png");
        runtime.Update(new(.405, .025, 0)); var recoil = runtime.CurrentRig.Animated["rightHand"];
        Assert.True(recoil.OffsetY < aim.OffsetY - 3); Assert.True(recoil.OffsetX > aim.OffsetX + 3);
        var recovered = runtime.Update(new(.52, .115, 0));
        Assert.DoesNotContain(recovered.Layers, l => l.Asset == "samples/muzzle-flash.png");
        Assert.Equal(aimLocal.OffsetX, runtime.CurrentRig.Animated["body"].Inverse().Compose(runtime.CurrentRig.Animated["rightHand"]).OffsetX, 1);
        Assert.Equal(7, CompanionWeapon.All.Select(w => w.Asset).Distinct().Count());
    }
}
