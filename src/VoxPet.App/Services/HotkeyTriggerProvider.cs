using VoxPet.Core.Legacy;
using VoxPet.Core.Triggers;

namespace VoxPet.App.Services;

/// <summary>등록된 Windows 키의 통지만 플랫폼 독립 pulse로 변환한다.</summary>
public static class HotkeyTriggerProvider
{
    public static TriggerEvent FromLegacy(uint key, uint modifiers) => new(TriggerKind.Hotkey,
        LegacyShortcutAdapter.Key(key), modifiers: LegacyShortcutAdapter.Modifiers(modifiers), isPulse: true);
}
