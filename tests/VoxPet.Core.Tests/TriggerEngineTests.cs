using VoxPet.Core.Actions;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Triggers;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class TriggerEngineTests
{
    [Fact]
    public void HotkeysMatchModifiersSuppressRepeatsAndReleaseWithoutModifiers()
    {
        var engine = new TriggerEngine([new("key", "wave", TriggerKind.Hotkey, "Q", modifiers: TriggerModifiers.Ctrl)]);
        Assert.Empty(engine.Update(0, 0, [new(TriggerKind.Hotkey, "q")]));
        Assert.Equal(new[] { "wave" }, engine.Update(1, 0, [new(TriggerKind.Hotkey, "q", modifiers: TriggerModifiers.Ctrl)]));
        Assert.Empty(engine.Update(2, 0, [new(TriggerKind.Hotkey, "q", modifiers: TriggerModifiers.Ctrl)]));
        Assert.Empty(engine.Update(3, 0, [new(TriggerKind.Hotkey, "q", pressed: false)]));
        Assert.Equal(new[] { "wave" }, engine.Update(4, 0, [new(TriggerKind.Hotkey, "q", modifiers: TriggerModifiers.Ctrl)]));
    }
    [Fact]
    public void RegisterHotkeyPulsesNeedNoReleaseAndUnknownKeysAreIgnored()
    {
        var engine = new TriggerEngine([new("key", "wave", TriggerKind.Hotkey, "1")]);
        Assert.Equal(new[] { "wave" }, engine.Update(0, 0, [new(TriggerKind.Hotkey, "1", isPulse: true)]));
        Assert.Equal(new[] { "wave" }, engine.Update(1, 0, [new(TriggerKind.Hotkey, "1", isPulse: true)]));
        Assert.Empty(engine.Update(2, 0, [new(TriggerKind.Hotkey, "unbound")]));
    }
    [Fact]
    public void VoiceTriggersOnRisingEdgeAndMuteRearmsIt()
    {
        var engine = new TriggerEngine([new("voice", "nod", TriggerKind.VoiceThreshold, threshold: .6)]);
        Assert.Empty(engine.Update(0, .59)); Assert.Equal(new[] { "nod" }, engine.Update(1, .6));
        Assert.Empty(engine.Update(2, 1)); Assert.Empty(engine.Update(3, double.NaN));
        Assert.Equal(new[] { "nod" }, engine.Update(4, .8)); Assert.Empty(engine.Update(5, .7));
        Assert.Empty(engine.Requests);
    }
    [Fact]
    public void IntervalDelaysAreCoalescedAndCanStartImmediately()
    {
        var engine = new TriggerEngine([new("interval", "wave", TriggerKind.Interval, intervalSeconds: 2)], 10);
        Assert.Empty(engine.Update(11.99, 0)); Assert.Equal(new[] { "wave" }, engine.Update(12, 0));
        Assert.Equal(new[] { "wave" }, engine.Update(100, 0)); Assert.Empty(engine.Update(101.99, 0));
        Assert.Equal(new[] { "wave" }, engine.Update(102, 0));
        Assert.Equal(TriggerKind.Interval, Assert.Single(engine.Requests).Kind);
        Assert.Equal("interval", engine.Requests[0].TriggerId);
        var immediate = new TriggerEngine([new("immediate", "idle-action", TriggerKind.Interval, intervalSeconds: 3, startImmediately: true)], 50);
        Assert.Equal(new[] { "idle-action" }, immediate.Update(50, 0));
    }
    [Fact]
    public void MouseManualDisabledAndDuplicateActionsHaveDeterministicOrder()
    {
        var engine = new TriggerEngine([new("mouse", "wave", TriggerKind.Mouse, button: "left"), new("manual", "wave", TriggerKind.Manual, "wave"),
            new("manual2", "Other", TriggerKind.Manual, "Other"), new("off", "disabled", TriggerKind.Mouse, button: "left", enabled: false)]);
        Assert.Equal(new[] { "wave", "Other" }, engine.Update(0, 0,
            [new(TriggerKind.Mouse, button: "left"), new(TriggerKind.Manual, "wave"), new(TriggerKind.Manual, "Other")]));
        Assert.Empty(engine.Update(1, 0, [new(TriggerKind.Manual, "other"), new(TriggerKind.Mouse, button: "right")]));
        Assert.Empty(engine.Update(2, 0, [new(TriggerKind.Mouse, button: "left", pressed: false)]));
    }
    [Fact]
    public void DeterministicReplayAndResetUseOnlyCallerTime()
    {
        TriggerDefinition[] definitions = [new("voice", "nod", TriggerKind.VoiceThreshold), new("interval", "wave", TriggerKind.Interval, intervalSeconds: 2)];
        var a = new TriggerEngine(definitions); var b = new TriggerEngine(definitions);
        for (int i = 0; i < 300; i++) Assert.Equal(a.Update(i / 10d, i % 10 / 10d), b.Update(i / 10d, i % 10 / 10d));
        a.Reset(30); Assert.Equal(new[] { "nod" }, a.Update(30, 1)); Assert.Equal(new[] { "wave" }, a.Update(32, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => a.Update(31, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => a.Update(double.PositiveInfinity, 0));
    }
    [Fact]
    public void LegacyBindingsPreserveModeEnabledIntervalsAndShortcutSemantics()
    {
        var settings = HandMotionSettings.Default(); var converted = LegacyHandMotionTriggerAdapter.Convert(settings);
        Assert.Equal(5, converted.Bindings.Count); Assert.Single(converted.EnabledTriggers);
        Assert.Equal(TriggerKind.Mouse, converted.EnabledTriggers.Single().Kind);
        var hotkey = settings with { Profiles = settings.Profiles.Select(p => p with { LeftClick = false }).ToArray() };
        var binding = LegacyHandMotionTriggerAdapter.Convert(hotkey).EnabledTriggers.Single();
        Assert.Equal("1", binding.Key); Assert.Equal(TriggerModifiers.Ctrl | TriggerModifiers.Alt, binding.Modifiers);
        var interval = settings with { Profiles = settings.Profiles.Select(p => p with { Mode = HandTriggerMode.Interval, IntervalSeconds = 7 }).ToArray() };
        Assert.Equal(7, LegacyHandMotionTriggerAdapter.Convert(interval).EnabledTriggers.Single().IntervalSeconds);
        var always = settings with { Profiles = settings.Profiles.Select(p => p with { Mode = HandTriggerMode.Always }).ToArray() };
        Assert.True(LegacyHandMotionTriggerAdapter.Convert(always).EnabledTriggers.Single().StartImmediately);
        Assert.Equal("f12", LegacyShortcutAdapter.Key(0x7b)); Assert.Equal("space", LegacyShortcutAdapter.Key(0x20));
        Assert.Equal(TriggerModifiers.Ctrl | TriggerModifiers.Shift, LegacyShortcutAdapter.Modifiers(6));
        Assert.Throws<ArgumentOutOfRangeException>(() => LegacyShortcutAdapter.Key(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => LegacyShortcutAdapter.Modifiers(8));
        var positioned = settings with { Profiles = settings.Profiles.Select(p => p with { X = 25, Y = -15, Scale = 1.3 }).ToArray() };
        var placement = LegacyHandMotionTriggerAdapter.Convert(positioned).Bindings[0].Transform;
        Assert.Equal(25, placement.X); Assert.Equal(-15, placement.Y); Assert.Equal(1.3, placement.ScaleX); Assert.Equal(1.3, placement.ScaleY);
    }
    [Fact]
    public void ModelsAreCopiedAndBrokenBindingsAreRejected()
    {
        TriggerDefinition[] definitions = [new("manual", "wave", TriggerKind.Manual, "wave")];
        var engine = new TriggerEngine(definitions); var binding = new ActionBinding("wave", definitions);
        var action = new ActionDefinition("wave", "흔들기", 10, [], triggers: definitions);
        definitions[0] = new("other", "other", TriggerKind.Manual, "other");
        Assert.Equal(new[] { "wave" }, engine.Update(0, 0, [new(TriggerKind.Manual, "wave")]));
        Assert.Equal("manual", binding.Triggers[0].Id); Assert.Equal("manual", action.Triggers[0].Id);
        Assert.Throws<NotSupportedException>(() => ((IList<TriggerDefinition>)binding.Triggers).Clear());
        Assert.Throws<ArgumentException>(() => new ActionBinding("wrong", action.Triggers));
        Assert.Throws<ArgumentException>(() => new ActionSettings([binding, binding]));
        Assert.Throws<ArgumentException>(() => new TriggerEngine([definitions[0], definitions[0]]));
        Assert.Throws<ArgumentException>(() => new TriggerDefinition("key", "wave", TriggerKind.Hotkey));
        Assert.Throws<ArgumentException>(() => new TriggerEvent(TriggerKind.Mouse, button: "unknown"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TriggerDefinition("key", "wave", TriggerKind.VoiceThreshold, threshold: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TriggerDefinition("key", "wave", TriggerKind.Interval, intervalSeconds: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TriggerEvent(TriggerKind.Hotkey, "q", modifiers: (TriggerModifiers)8));
    }
}
