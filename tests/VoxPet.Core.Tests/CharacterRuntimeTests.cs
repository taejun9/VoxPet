using System.Text.Json;
using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Motion;
using VoxPet.Core.Rendering;
using VoxPet.Core.Services;
using VoxPet.Core.Triggers;
using Xunit;
using CharacterDefinition = VoxPet.Core.Character.CharacterDefinition;

namespace VoxPet.Core.Tests;

public sealed class CharacterRuntimeTests
{
    private static CharacterDefinition Sample(IEnumerable<ActionDefinition>? actions = null, IEnumerable<AttachmentDefinition>? attachments = null, MotionProfile? motion = null) => new(
        [new("body", "body", "body.png", "body", 100), new("head", "head", "head.png", "head", 200)],
        [new("root"), new("body", parent: "root"), new("head", parent: "body"), new("eyes", parent: "head"), new("face", parent: "head"), new("mouth", parent: "head")],
        mouth: new(Enumerable.Range(0, 5).Select(i => $"mouth-{i}.png")), expressions: [new("neutral", "open.png", "closed.png"), new("happy", "happy.png", "closed.png")],
        actions: actions, attachments: attachments, motion: motion ?? new(blinkEnabled: false, idleEnabled: false, headBounce: 0, headRotation: 0));
    [Fact]
    public void IdleVoiceActionIdlePipelineOwnsMouthAndPriority()
    {
        var gun = new ActionDefinition("gun", "총", 1000, [new(0, ActionEventKind.Transform, "head", transform: new(rotation: -20))], 80);
        var runtime = new CharacterRuntime(Sample([gun], motion: new(blinkEnabled: false, idleEnabled: false, headRotation: 10)), new Random(1));
        var idle = runtime.Update(new(0, 0, 0)); Assert.Equal(0, runtime.MouthFrame); Assert.Null(runtime.State.ActionId);
        var voice = runtime.Update(new(.1, .1, .5)); Assert.Equal(2, runtime.MouthFrame);
        Assert.Equal(-2.5, voice.Layers.Single(l => l.Id == "part-body").Y, 10);
        runtime.RequestAction("gun"); var action = runtime.Update(new(.2, .1, 1));
        Assert.Equal("gun", runtime.State.ActionId); Assert.Equal(-20, action.Layers.Single(l => l.Id == "part-head").Rotation, 10);
        var ended = runtime.Update(new(1.2, 1, 0)); Assert.Null(runtime.State.ActionId); Assert.Equal(0, runtime.MouthFrame);
        Assert.Equal(0, ended.Layers.Single(l => l.Id == "part-head").Rotation, 10); Assert.Equal(0, idle.Layers.Single(l => l.Id == "part-body").Y);
    }
    [Fact]
    public void MouthSupportsAllFrameCountsAndAwayFromZeroRounding()
    {
        for (int count = 1; count <= 16; count++)
        {
            var mouth = new MouthDefinition(Enumerable.Range(0, count).Select(i => $"mouth-{i}.png"), curve: "linear");
            int previous = -1;
            for (int i = 0; i <= 1000; i++) { int frame = MouthController.Select(mouth, i / 1000d); Assert.InRange(frame, 0, count - 1); Assert.True(frame >= previous); previous = frame; }
            Assert.Equal(count - 1, previous); Assert.Equal(0, MouthController.Select(mouth, double.NaN));
        }
        Assert.Equal(3, MouthController.Select(new(Enumerable.Repeat("mouth.png", 5), curve: "linear"), .625));
        Assert.Equal(1, MouthController.Select(new(Enumerable.Repeat("mouth.png", 3), curve: "legacy3"), .2));
        Assert.Equal(7, MouthController.Select(new(Enumerable.Repeat("mouth.png", 8), curve: "legacy8"), .90));
    }
    [Fact]
    public void AutomaticActionsQueueFairlyAndManualInputPreemptsImmediately()
    {
        var actions = new[] { "a", "b", "c" }.Select(id => new ActionDefinition(id, id, 1000, [], 30,
            triggers: [new(id + "-timer", id, TriggerKind.Interval, intervalSeconds: 2, startImmediately: true)])).Append(new("weapon", "무기", 1000, [], 80)).ToArray();
        var runtime = new CharacterRuntime(Sample(actions));
        foreach (var (time, id) in new[] { (0d, "a"), (1d, "b"), (2d, "c"), (3d, "a") }) { runtime.Update(new(time, 0, 0)); Assert.Equal(id, runtime.State.ActionId); }
        runtime.RequestAction("weapon"); runtime.Update(new(3.1, .1, 0)); Assert.Equal("weapon", runtime.State.ActionId);
        runtime.Update(new(100, 96.9, 0)); Assert.Equal("b", runtime.State.ActionId);
        runtime.StopActions(); runtime.Update(new(100.1, .1, 0)); Assert.Null(runtime.State.ActionId);
    }
    [Fact]
    public void ExpressionsAttachmentsEffectsAndImageSizesAreFullyComposed()
    {
        var hat = new AttachmentDefinition("hat", "모자", "hat", [new("hat", "hat.png", "head", placement: new(width: 20, height: 30))]);
        var action = new ActionDefinition("test", "시험", 1000, [new(0, ActionEventKind.Expression, value: "happy"), new(0, ActionEventKind.Attachment, value: "hat"),
            new(100, ActionEventKind.Effect, "head", "pulse.png", durationMs: 100)]);
        var runtime = new CharacterRuntime(Sample([action], [hat]), dimensions: new Dictionary<string, AssetDimensions> { ["pulse.png"] = new(16, 8) });
        runtime.RequestAction("test"); var frame = runtime.Update(new(0, 0, 0, true));
        Assert.Equal("happy", runtime.State.ExpressionId); Assert.Equal("closed.png", frame.Layers.Single(l => l.Id == "face-eyes").Asset);
        var accessory = frame.Layers.Single(l => l.Asset == "hat.png"); Assert.Equal(20, accessory.Width); Assert.Equal(30, accessory.Height);
        var pulse = runtime.Update(new(.15, .15, 0)).Layers.Single(l => l.Asset == "pulse.png"); Assert.Equal(16, pulse.Width); Assert.Equal(8, pulse.Height);
        Assert.DoesNotContain(runtime.Update(new(.2, .05, 0)).Layers, l => l.Asset == "pulse.png");
        var after = runtime.Update(new(1, .8, 0)); Assert.DoesNotContain(after.Layers, l => l.Asset == "hat.png"); Assert.Equal("neutral", runtime.State.ExpressionId);
        runtime.SetExpression("happy"); runtime.Attachments.SetEnabled("hat", true); runtime.Update(new(2, 1, 0)); Assert.Contains("hat", runtime.State.AttachmentIds);
    }
    [Fact]
    public void LegacyTemplateMatchesOldCompositionAcrossFramesMouthAndBlink()
    {
        var source = JsonSerializer.Deserialize<ImageSet>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ImageSetTemplate", "imageset.json")), ImageSet.JsonOptions)!;
        var runtime = new CharacterRuntime(LegacyImageSetAdapter.Convert(source), new Random(1)); var old = new ImageSetPlayer(source);
        runtime.Attachments.SetEnabled("outfit-0", true); runtime.Attachments.SetEnabled("weapon-0", true); runtime.RequestAction("action-0"); old.Play(0, 0);
        foreach (var (time, level, blink, expression) in new[] { (0d, 0d, false, ExpressionKind.Neutral), (.081, .5, false, ExpressionKind.Happy),
            (.161, 1d, true, ExpressionKind.Happy), (.241, 1d, false, ExpressionKind.Neutral), (.341, 0d, false, ExpressionKind.Neutral) })
        {
            runtime.SetExpression(expression.ToString().ToLowerInvariant()); var frame = runtime.Update(new(time, 0, level, blink));
            var expected = old.Compose(time, level, blink, expression, 0, 0);
            Assert.Equal(expected.Select(l => l.File), frame.Layers.Select(l => l.Asset));
            for (int i = 0; i < expected.Count; i++)
            { Assert.Equal(expected[i].Offset.X, frame.Layers[i].X, 10); Assert.Equal(expected[i].Offset.Y, frame.Layers[i].Y, 10); }
            Assert.True(frame.ViewportTransform.Y <= 0); Assert.Equal(source.CanvasSize, frame.CanvasWidth);
        }
    }
    [Fact]
    public void HiddenHeadAndArmRemoveDependentFaceAndSleeveLayers()
    {
        var source = JsonSerializer.Deserialize<ImageSet>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ImageSetTemplate", "imageset.json")), ImageSet.JsonOptions)!;
        source = source with { Actions = [source.Actions[0] with { Frames = [source.Actions[0].Frames[0] with { Replace = new() { [ImagePart.Head] = null, [ImagePart.ArmR] = null } }] }] };
        var runtime = new CharacterRuntime(LegacyImageSetAdapter.Convert(source)); runtime.Attachments.SetEnabled("outfit-0", true); runtime.RequestAction("action-0");
        var frame = runtime.Update(new(0, 0, 1)); Assert.DoesNotContain(frame.Layers, l => l.Id.StartsWith("face-"));
        Assert.DoesNotContain(frame.Layers, l => l.Asset.StartsWith("sleeve"));
    }
    [Fact]
    public void RepeatedManualPreemptionKeepsAutomaticQueueBoundedAndExpressionFallbackMatchesOutput()
    {
        var action = new ActionDefinition("wave", "흔들기", 1000, [], triggers: [new("timer", "wave", TriggerKind.Interval, intervalSeconds: .1, startImmediately: true)]);
        var runtime = new CharacterRuntime(Sample([action]));
        for (int i = 0; i < 1000; i++)
        {
            runtime.RequestAction("wave"); runtime.Update(new(i / 10d, .1, 0)); Assert.InRange(runtime.PendingAutomaticCount, 0, 1);
        }
        var onlyHappy = new CharacterDefinition([new("body", "body", "body.png")], expressions: [new("happy", "happy.png", "closed.png")]);
        var fallback = new CharacterRuntime(onlyHappy); fallback.Update(new(0, 0, 0)); Assert.Equal("happy", fallback.State.ExpressionId);
    }
    [Fact]
    public void InputSnapshotsAreCopiedAndReplayIsDeterministic()
    {
        TriggerEvent[] events = [new(TriggerKind.Manual, "wave")]; var snapshot = new InputSnapshot(events); events[0] = new(TriggerKind.Manual, "unknown");
        var action = new ActionDefinition("wave", "흔들기", 1000, [new(0, ActionEventKind.Transform, "head", transform: new(rotation: 15))]);
        var a = new CharacterRuntime(Sample([action]), new Random(7)); var b = new CharacterRuntime(Sample([action]), new Random(7));
        for (int i = 0; i < 120; i++)
        {
            var input = new RuntimeInput(i / 60d, 1 / 60d, i % 10 / 10d, input: i == 0 ? snapshot : null);
            var first = a.Update(input); var second = b.Update(input);
            Assert.Equal(first.Layers.Select(l => (l.Id, l.Asset, l.Matrix)), second.Layers.Select(l => (l.Id, l.Asset, l.Matrix)));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => a.Update(new(0, 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RuntimeInput(0, 0, double.NaN));
        Assert.Throws<ArgumentException>(() => new InputSnapshot([null!]));
        Assert.Throws<ArgumentException>(() => a.RequestAction("missing")); Assert.Throws<ArgumentException>(() => a.SetExpression("missing"));
    }
}
