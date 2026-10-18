using VoxPet.Core.Actions;
using VoxPet.Core.Character;
using VoxPet.Core.Packages;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class EmotiveMotionTests
{
    private static ActionTimelineEvent Key(double t, double x) => new(t, ActionEventKind.Transform, "hand", transform: new(x: x, y: x * .5, scaleX: 1 + x / 1000, rotation: x));
    private static ActionEngine Engine(params ActionTimelineEvent[] keys)
    {
        var engine = new ActionEngine();
        engine.TryPlay(new("flow", "Flow", 2000, keys, interpolateTransforms: true, transformInterpolation: ActionInterpolation.MonotoneCubic), 0);
        return engine;
    }
    [Fact]
    public void PassingAKeyKeepsVelocityWhileArrivalHoldAndReversalStayBounded()
    {
        var engine = Engine(Key(0, 0), Key(400, 50), Key(1000, 150), Key(1400, 150), Key(2000, 0));
        double x0 = engine.Update(.399).Transforms["hand"].X, x1 = engine.Update(.4).Transforms["hand"].X, x2 = engine.Update(.401).Transforms["hand"].X;
        Assert.InRange(x1 - x0, .1, .2); Assert.InRange(Math.Abs((x2 - x1) - (x1 - x0)), 0, .002);
        Assert.Equal(150, engine.Update(1.2).Transforms["hand"].X);
        double last = 150;
        for (int i = 1401; i < 2000; i++)
        { double value = engine.Update(i / 1000d).Transforms["hand"].X; Assert.InRange(value, 0, last); last = value; }
        Assert.InRange(last, 0, .002);
    }
    [Fact]
    public void DuplicateKeysPoseResetAndMirrorsDoNotLeakAcrossCurveSegments()
    {
        var engine = Engine(Key(0, 0), Key(400, 10), Key(400, 50), Key(1000, 100));
        Assert.Equal(50, engine.Update(.4).Transforms["hand"].X);
        var legacy = new ActionEngine(); legacy.TryPlay(new("legacy", "Legacy", 2000, [Key(0, 0), Key(400, 10), Key(400, 50), Key(1000, 100)], interpolateTransforms: true), 0);
        Assert.Equal(5, legacy.Update(.2).Transforms["hand"].X); Assert.Equal(50, legacy.Update(.4).Transforms["hand"].X);
        var poses = new[] { new PoseDefinition("reset", transforms: new Dictionary<string, Transform2D> { ["hand"] = new(x: -100) }) };
        var a = new ActionDefinition("move", "Move", 2000, [Key(0, 0), Key(500, 20), new(800, ActionEventKind.Pose, value: "reset"), Key(1200, -50), Key(1800, 0), new(1900, ActionEventKind.Transform, "hand", transform: new(scaleX: -1))], interpolateTransforms: true, transformInterpolation: ActionInterpolation.MonotoneCubic);
        engine = new(poses); engine.TryPlay(a, 0);
        Assert.Equal(20, engine.Update(.79).Transforms["hand"].X); Assert.Equal(-100, engine.Update(.8).Transforms["hand"].X);
        Assert.InRange(engine.Update(.9).Transforms["hand"].X, -100, -50);
        Assert.Equal(1, engine.Update(1.899).Transforms["hand"].ScaleX); Assert.Equal(-1, engine.Update(1.9).Transforms["hand"].ScaleX);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActionDefinition("bad", "Bad", 1000, [], transformInterpolation: (ActionInterpolation)9));
    }
    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void EveryGestureFlowsIntoIdleAndKeepsTwoHandContactThroughoutTheSweep(string id)
    {
        var p = new VoxPetPackageLoader().Load(Path.Combine(AppContext.BaseDirectory, "Samples", "companion-" + id + ".voxpet"));
        var exported = new VoxPetPackageLoader().Load(new MemoryStream(new VoxPetPackageExporter().Export(p)));
        Assert.All(exported.Character.Actions, a => Assert.Equal(ActionInterpolation.MonotoneCubic, a.TransformInterpolation));
        foreach (var action in p.Character.Actions)
        {
            var engine = new ActionEngine(); engine.TryPlay(action, 0);
            var first = engine.Update(0); var tail = engine.Update((action.DurationMs - .01) / 1000);
            foreach (var pair in tail.Transforms.Where(p => p.Key != "arrow"))
            {
                var start = first.Transforms[pair.Key]; var end = pair.Value;
                Assert.InRange(Math.Abs(end.X - start.X) + Math.Abs(end.Y - start.Y) + Math.Abs(end.Rotation - start.Rotation), 0, .001);
                Assert.InRange(Math.Abs(end.ScaleX - start.ScaleX) + Math.Abs(end.ScaleY - start.ScaleY), 0, .00001);
            }
            Assert.Empty(engine.Update(action.DurationMs / 1000).Transforms);
            Assert.Contains(action.Timeline, e => e.Target == "head" && Math.Abs(e.Transform?.Rotation ?? 0) > .5);
        }
        var runtime = p.CreateRuntime(); runtime.RequestAction("two-hand"); runtime.Update(new(0, 0, 0));
        for (int ms = 220; ms <= 940; ms += 7)
        {
            runtime.Update(new(ms / 1000d, .007, 0));
            var rig = runtime.CurrentRig.Animated;
            var headLocal = runtime.CurrentRig.Static["body"].Compose(rig["body"].Inverse()).Compose(rig["head"]).Compose(runtime.CurrentRig.Static["head"].Inverse());
            double nx = id == "pelly" ? 270 : 256, ny = id == "pelly" ? 302 : 285;
            var neck = headLocal.Map(nx, ny); Assert.InRange(Math.Abs(nx - neck.X), 0, .01); Assert.InRange(Math.Abs(neck.Y - ny), 0, 1.8);
            var contact = rig["rightHand"].Map(0, 23); var support = rig["leftHand"].Map(0, 0);
            Assert.InRange(Math.Sqrt(Math.Pow(contact.X - support.X, 2) + Math.Pow(contact.Y - support.Y, 2)), 0, .35);
        }
    }
    [Fact]
    public void CharacterChoreographyHasDifferentTimingAndSecondaryMotion()
    {
        var packs = new[] { "neulbo", "pelly" }.Select(id => new VoxPetPackageLoader().Load(Path.Combine(AppContext.BaseDirectory, "Samples", "companion-" + id + ".voxpet"))).ToArray();
        foreach (var action in packs[0].Character.Actions)
        {
            var other = packs[1].Character.Actions.Single(a => a.Id == action.Id);
            var a = new ActionEngine(); var b = new ActionEngine(); a.TryPlay(action, 0); b.TryPlay(other, 0);
            Assert.NotEqual(a.Update(.17).Transforms["head"], b.Update(.17).Transforms["head"]);
            Assert.NotEqual(a.Update(.31).Transforms["body"], b.Update(.31).Transforms["body"]);
        }
    }
}
