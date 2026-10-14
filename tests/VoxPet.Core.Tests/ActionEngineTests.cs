using VoxPet.Core.Actions;
using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class ActionEngineTests
{
    private static ActionDefinition Action(string id = "wave", int priority = 50, ActionPlayback? playback = null) => new(id, id, 1000,
        [new(0, ActionEventKind.Transform, "head", transform: new(rotation: 10)), new(500, ActionEventKind.Transform, "head", transform: new(rotation: -10))], priority, playback);
    [Fact]
    public void EverySupportedEventIsAppliedAndTransientEffectsExpireAtBoundary()
    {
        var pose = new PoseDefinition("ready", sprites: new Dictionary<string, string?> { ["arm"] = "arm-ready.png" });
        var action = new ActionDefinition("gun", "총", 1000,
        [new(0, ActionEventKind.Pose, value: "ready"), new(100, ActionEventKind.Transform, "head", transform: new(2, -5, rotation: 4)),
         new(200, ActionEventKind.Visibility, "arm", enabled: false), new(250, ActionEventKind.Sprite, "body", "body-fire.png"),
         new(260, ActionEventKind.Effect, "rightHand", "muzzle.png", durationMs: 80), new(300, ActionEventKind.Expression, value: "happy"),
         new(320, ActionEventKind.Attachment, value: "hat", enabled: true), new(330, ActionEventKind.Sound, value: "reserved.wav")], 80);
        var engine = new ActionEngine([pose]); Assert.True(engine.TryPlay(action, 10));
        var start = engine.Update(10); Assert.Equal("arm-ready.png", start.Sprites["arm"]); Assert.Equal("ready", start.PoseId);
        var state = engine.Update(10.33); Assert.Equal(new Transform2D(2, -5, rotation: 4), state.Transforms["head"]);
        Assert.False(state.Visibility["arm"]); Assert.Equal("body-fire.png", state.Sprites["body"]);
        Assert.Equal("happy", state.ExpressionId); Assert.True(state.Attachments["hat"]);
        Assert.Equal("muzzle.png", Assert.Single(state.Effects).Asset);
        Assert.Empty(engine.Update(10.341).Effects); Assert.False(engine.Update(11).IsPlaying);
        Assert.Null(engine.Current.ActionId); Assert.Empty(engine.Current.Sprites);
    }
    [Fact]
    public void EffectsExpireExactlyAtTheirEndOnTheCallerTimeAxis()
    {
        var action = new ActionDefinition("pulse", "효과", 1000, [new(260, ActionEventKind.Effect, "hand", "pulse.png", durationMs: 80)]);
        var engine = new ActionEngine(); engine.TryPlay(action, 0);
        Assert.Single(engine.Update(.339).Effects); Assert.Empty(engine.Update(.340).Effects);
    }
    [Fact]
    public void PriorityAndInterruptibilityArbitrateWithoutRestartingRejectedActions()
    {
        var engine = new ActionEngine(); Assert.True(engine.TryPlay(Action("gun", 80), 0));
        Assert.False(engine.TryPlay(Action("wave", 50), .2)); Assert.Equal("gun", engine.Current.ActionId); Assert.Equal(.2, engine.Current.Progress, 10);
        Assert.True(engine.TryPlay(Action("other-gun", 80), .3)); Assert.Equal(0, engine.Current.Progress);
        Assert.True(engine.TryPlay(Action("locked", 80, new(Interruptible: false)), .4));
        Assert.False(engine.TryPlay(Action("higher", 90), .5)); Assert.Equal("locked", engine.Current.ActionId);
        Assert.True(engine.TryPlay(Action("system", ActionPriority.System), .6));
        Assert.False(engine.TryPlay(Action("system", ActionPriority.System), .7, restart: false));
        Assert.True(engine.TryPlay(Action("system", ActionPriority.System), .8)); Assert.Equal(0, engine.Current.Progress);
    }
    [Fact]
    public void OnceRestoresIdleButHoldRetainsFinalPoseAndAllowsNextAction()
    {
        var engine = new ActionEngine(); engine.TryPlay(Action(), 0); Assert.Equal(-10, engine.Update(.5).Transforms["head"].Rotation);
        Assert.Null(engine.Update(1).ActionId);
        engine.TryPlay(Action("hold", playback: new(ReturnToIdle: false)), 1);
        var held = engine.Update(2); Assert.False(held.IsPlaying); Assert.Equal("hold", held.ActionId); Assert.Equal(-10, held.Transforms["head"].Rotation);
        Assert.Equal(1, held.Progress); Assert.True(engine.TryPlay(Action("new", 10), 2.1));
        engine.Stop(); Assert.Null(engine.Current.ActionId);
    }
    [Fact]
    public void LoopWrapAndVeryDelayedUpdatesOnlyEvaluateCurrentCycle()
    {
        var engine = new ActionEngine(); engine.TryPlay(Action(playback: new(ActionPlaybackMode.Loop)), 0);
        Assert.Equal(10, engine.Update(1).Transforms["head"].Rotation);
        Assert.Equal(-10, engine.Update(1000000.75).Transforms["head"].Rotation);
        var huge = engine.Update(double.MaxValue); Assert.True(huge.IsPlaying); Assert.InRange(huge.Progress, 0, 1);
        Assert.True(double.IsFinite(huge.Transforms["head"].Rotation));
    }
    [Fact]
    public void PoseSnapshotsResetPriorOverridesAndHideSpritesWithNull()
    {
        var poses = new[] { new PoseDefinition("first", "fire", new Dictionary<string, string?> { ["arm"] = "fire.png" }),
            new PoseDefinition("second", "fire", new Dictionary<string, string?> { ["head"] = null }) };
        var action = new ActionDefinition("test", "시험", 1000, [new(0, ActionEventKind.Pose, value: "first"), new(100, ActionEventKind.Pose, value: "second")]);
        var engine = new ActionEngine(poses); engine.TryPlay(action, 0);
        var state = engine.Update(.2); Assert.Equal("second", state.PoseId); Assert.Equal("fire", state.PoseName);
        Assert.False(state.Sprites.ContainsKey("arm")); Assert.Null(state.Sprites["head"]);
    }
    [Fact]
    public void RepeatedEvaluationIsDeterministicAndSnapshotsRemainIndependent()
    {
        var a = new ActionEngine(); var b = new ActionEngine(); var action = Action(); a.TryPlay(action, 5); b.TryPlay(action, 5);
        var old = a.Update(5.2); var current = a.Update(5.7); var same = b.Update(5.7);
        Assert.Equal(10, old.Transforms["head"].Rotation); Assert.Equal(-10, current.Transforms["head"].Rotation);
        Assert.Equal(current.Transforms.OrderBy(p => p.Key), same.Transforms.OrderBy(p => p.Key));
        Assert.Equal(current.Progress, same.Progress);
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, Transform2D>)current.Transforms).Clear());
        Assert.Equal(current.Transforms.OrderBy(p => p.Key), a.Update(5.7).Transforms.OrderBy(p => p.Key));
    }
    [Fact]
    public void LegacyConvertedFrameTimingUsesSameGenericEngine()
    {
        var source = System.Text.Json.JsonSerializer.Deserialize<ImageSet>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ImageSetTemplate", "imageset.json")), ImageSet.JsonOptions)!;
        var definition = LegacyImageSetAdapter.Convert(source); var engine = new ActionEngine(definition.Poses); var legacy = new ImageSetPlayer(source);
        engine.TryPlay(definition.Actions[0], 0); legacy.Play(0, 0);
        foreach (double time in new[] { 0, .079, .081, .159, .161, .239, .241, .339, .341 })
        {
            var old = legacy.Frame(time); var state = engine.Update(time);
            Assert.Equal(old?.Pose, state.PoseName);
            if (old is not null) Assert.Equal(old.Replace[ImagePart.ArmR], state.Sprites["arm-right"]);
        }
    }
    [Fact]
    public void InvalidDefinitionsAndBackwardTimeAreRejectedBeforePlayback()
    {
        var engine = new ActionEngine(); engine.TryPlay(Action(), 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Update(.9));
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Update(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActionDefinition("a", "동작", .00001, []));
        Assert.Throws<ArgumentException>(() => engine.TryPlay(new("a", "동작", 10, [new(0, ActionEventKind.Pose, value: "missing")]), 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActionDefinition("a", "동작", 10, [], priority: 101));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActionDefinition("a", "동작", 10, [], playback: new((ActionPlaybackMode)99)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActionTimelineEvent(double.PositiveInfinity, ActionEventKind.Pose, value: "idle"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ActionTimelineEvent(0, ActionEventKind.Effect, "head", "effect.png", durationMs: 0));
        Assert.Throws<ArgumentException>(() => new ActionDefinition("a", "동작", 10, [new(5, ActionEventKind.Effect, "head", "effect.png", durationMs: 6)]));
        Assert.Equal("wave", engine.Current.ActionId);
    }
    [Fact]
    public void TerminalPoseIsLegalAndHandSchedulerRetainsRewindAndStopContract()
    {
        var action = new ActionDefinition("terminal", "종료", 1000, [new(1000, ActionEventKind.Pose, value: "idle")], playback: new(ReturnToIdle: false));
        var engine = new ActionEngine(); engine.TryPlay(action, 0); Assert.Equal("idle", engine.Update(1).PoseName);
        var scheduler = new HandMotionScheduler(); scheduler.Configure(HandMotionSettings.Default(), 5);
        Assert.True(scheduler.Trigger(HandGesture.Claw, 0)); Assert.Equal(0, scheduler.Update(1)!.Progress);
        Assert.False(scheduler.Trigger(HandGesture.Claw, 4)); scheduler.Stop(); Assert.Null(scheduler.Update(10));
    }
}
