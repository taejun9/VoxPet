using VoxPet.Core.Actions;
using VoxPet.Core.Character;
using VoxPet.Core.Models;
using VoxPet.Core.Services;
using VoxPet.Core.Triggers;

namespace VoxPet.Core.Legacy;

public static class LegacyHandMotionTriggerAdapter
{
    public static ActionSettings Convert(HandMotionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings); settings.Validate();
        return new(settings.Profiles.OrderBy(profile => profile.Gesture).Select(profile =>
        {
            string action = profile.Gesture.ToString();
            var trigger = profile.Mode switch
            {
                HandTriggerMode.Always => new TriggerDefinition(action + "-always", action, TriggerKind.Interval, intervalSeconds: HandMotionScheduler.Duration, startImmediately: true),
                HandTriggerMode.Interval => new TriggerDefinition(action + "-interval", action, TriggerKind.Interval, intervalSeconds: profile.IntervalSeconds),
                _ when profile.LeftClick => new TriggerDefinition(action + "-mouse", action, TriggerKind.Mouse, button: "left"),
                _ => new TriggerDefinition(action + "-hotkey", action, TriggerKind.Hotkey, LegacyShortcutAdapter.Key(profile.Shortcut.Key), modifiers: LegacyShortcutAdapter.Modifiers(profile.Shortcut.Modifiers))
            };
            return new ActionBinding(action, [trigger], profile.Enabled, new Transform2D(profile.X, profile.Y, profile.Scale, profile.Scale));
        }));
    }
}
