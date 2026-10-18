using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using VoxPet.Core.Actions;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class AvatarPerformanceTests
{
    [Fact]
    public void DefaultInputUsesTheCompanionTimelineAndKeepsLegacyFallback()
    {
        var pack = new VoxPetPackageLoader().Load(Path.Combine(AppContext.BaseDirectory, "Samples", "companion-neulbo.voxpet"));
        string[] ids = ["claw", "toy-gun", "heart", "toy-sword", "magic-wand"];
        var map = Enum.GetValues<HandGesture>().ToDictionary(g => g, g => pack.Character.Actions.Single(a => a.Id == ids[(int)g]));
        var engine = new ActionEngine(); var scheduler = new HandMotionScheduler(engine);
        scheduler.Configure(HandMotionSettings.Default(), 0); scheduler.UseActions(map);
        Assert.True(scheduler.Trigger(HandGesture.Claw, 0)); Assert.Same(map[HandGesture.Claw], engine.Definition);
        Assert.Equal(.3, scheduler.Update(.3)!.Progress, 8); Assert.NotEmpty(engine.Current.Transforms);
        Assert.False(scheduler.Trigger(HandGesture.Claw, .31));
        scheduler.UseActions(null); Assert.False(engine.Current.IsPlaying);
        Assert.True(scheduler.Trigger(HandGesture.Claw, .4)); Assert.Equal("Claw", engine.Current.ActionId);
        Assert.Equal(.3 / 1.4, scheduler.Update(.7)!.Progress, 8);
    }
    [Fact]
    public void PartialInputMappingCannotCorruptAnAlreadyPlayingAction()
    {
        var engine = new ActionEngine(); var scheduler = new HandMotionScheduler(engine);
        scheduler.Configure(HandMotionSettings.Default(), 0); Assert.True(scheduler.Trigger(HandGesture.Claw, 0));
        var previous = engine.Definition;
        Assert.Throws<ArgumentException>(() => scheduler.UseActions(new Dictionary<HandGesture, ActionDefinition>()));
        Assert.Same(previous, engine.Definition); Assert.True(engine.Current.IsPlaying);
    }
    private static CharacterRuntime Create(string id, bool live = true)
    {
        var sample = new VoxPetPackageLoader().Load(Path.Combine(AppContext.BaseDirectory, "Samples", "companion-" + id + ".voxpet"));
        var processor = new LegacySpriteSheetRuntime { Samples = sample.Character, LivePerformance = live, PreserveForegroundWing = id == "pelly", Rig = CharacterRig.Default(id) };
        return new(LegacySpriteSheetAdapter.Convert(3, samples: sample.Character), processor: processor);
    }
    private static double Distance(AffineTransform2D a, AffineTransform2D b) => Math.Sqrt(Math.Pow(a.OffsetX - b.OffsetX, 2) + Math.Pow(a.OffsetY - b.OffsetY, 2));
    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void IdleBreathAndHeadActIndependentlyWithBothFeetPlanted(string id)
    {
        var live = Create(id); live.Update(new(0, 0, 0)); var first = live.CurrentRig.Animated["head"];
        bool moved = false;
        for (int i = 1; i <= 600; i++)
        {
            live.Update(new(i / 60d, 1 / 60d, 0)); var rig = live.CurrentRig;
            moved |= Distance(first, rig.Animated["head"]) > .25;
            foreach (string foot in new[] { "leftFoot", "rightFoot" }) Assert.InRange(Distance(rig.Static[foot], rig.Animated[foot]), 0, .0000001);
            Assert.InRange(rig.Animated["head"].OffsetX, rig.Static["head"].OffsetX - 2, rig.Static["head"].OffsetX + 2);
            Assert.InRange(rig.Animated["head"].OffsetY, rig.Static["head"].OffsetY - 2, rig.Static["head"].OffsetY + 2);
        }
        Assert.True(moved);
    }
    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void NineActionsRetainAPlantedStanceAndSharedGripGeometry(string id)
    {
        foreach (string action in new[] { "claw", "heart", "toy-gun", "toy-sword", "magic-wand", "firearm", "bow", "one-hand", "two-hand" })
        {
            var live = Create(id); live.RequestAction(action); live.Update(new(0, 0, 0));
            for (int i = 1; i <= 60; i++)
            {
                var frame = live.Update(new(i * .02, .02, .6)); var rig = live.CurrentRig;
                foreach (string foot in new[] { "leftFoot", "rightFoot" }) Assert.InRange(Distance(rig.Static[foot], rig.Animated[foot]), 0, .0000001);
                if (action == "two-hand" && i is >= 12 and <= 46)
                {
                    var support = rig.Animated["rightHand"].Map(0, 23); var hand = rig.Animated["leftHand"].Map(0, 0);
                    Assert.InRange(Math.Sqrt(Math.Pow(support.X - hand.X, 2) + Math.Pow(support.Y - hand.Y, 2)), 0, .35);
                }
                Assert.All(frame.Layers, l => Assert.True(double.IsFinite(l.Matrix.OffsetX) && double.IsFinite(l.Matrix.OffsetY)));
            }
        }
    }
    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void StopRestartAndRetargetKeepTheCurrentPoseBeforeSettling(string id)
    {
        var live = Create(id); live.RequestAction("one-hand"); live.Update(new(0, 0, 0));
        for (int i = 1; i <= 30; i++) live.Update(new(i / 60d, 1 / 60d, 0));
        var before = live.CurrentRig.Animated["rightHand"];
        live.StopActions(); live.Update(new(.5, 0, 0));
        Assert.InRange(Distance(before, live.CurrentRig.Animated["rightHand"]), 0, .0000001);
        live.Update(new(.5001, .0001, 0)); Assert.InRange(Distance(before, live.CurrentRig.Animated["rightHand"]), 0, .15);
        var midway = live.CurrentRig.Animated["rightHand"];
        live.RequestAction("heart"); live.Update(new(.5001, 0, 0));
        Assert.InRange(Distance(midway, live.CurrentRig.Animated["rightHand"]), 0, .0000001);
        live.Update(new(.64, .1399, 0)); var held = live.CurrentRig.Animated["rightHand"];
        live.RequestAction("heart"); live.Update(new(.64, 0, 0));
        Assert.InRange(Distance(held, live.CurrentRig.Animated["rightHand"]), 0, .0000001);
        live.StopActions(); live.Update(new(.64, 0, 0)); live.Update(new(1, .36, 0));
        var raw = Create(id, false); raw.Update(new(1, 1, 0));
        // Breathing adds a small body offset; the dramatic interrupted arm pose is gone.
        Assert.InRange(Distance(raw.CurrentRig.Animated["rightHand"], live.CurrentRig.Animated["rightHand"]), 0, 2);
    }
    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void VoiceMotionHasNoImmediateBodyJumpAndHandlesVeryLongPauses(string id)
    {
        var live = Create(id); live.Update(new(0, 0, 0)); var raw = Create(id, false);
        live.Update(new(.001, .001, 1)); raw.Update(new(.001, .001, 1));
        Assert.InRange(Distance(raw.CurrentRig.Animated["body"], live.CurrentRig.Animated["body"]), 0, .005);
        live.Update(new(1e200, 1e200, 0));
        Assert.All(live.CurrentRig.Animated.Values, m => Assert.True(double.IsFinite(m.OffsetX) && double.IsFinite(m.OffsetY)));
    }
}
