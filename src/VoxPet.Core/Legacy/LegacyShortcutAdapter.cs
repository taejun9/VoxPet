using VoxPet.Core.Triggers;

namespace VoxPet.Core.Legacy;

/// <summary>기존 저장 코드의 순수 데이터 변환. 플랫폼 API나 실제 키 상태 접근은 없다.</summary>
public static class LegacyShortcutAdapter
{
    public static string Key(uint key) => key switch
    {
        >= 0x41 and <= 0x5a or >= 0x30 and <= 0x39 => ((char)key).ToString().ToLowerInvariant(),
        >= 0x70 and <= 0x87 => "f" + (key - 0x70 + 1),
        0x20 => "space",
        _ => throw new ArgumentOutOfRangeException(nameof(key))
    };
    public static TriggerModifiers Modifiers(uint flags)
    {
        if ((flags & ~7u) != 0) throw new ArgumentOutOfRangeException(nameof(flags));
        return ((flags & 1) != 0 ? TriggerModifiers.Alt : TriggerModifiers.None) |
            ((flags & 2) != 0 ? TriggerModifiers.Ctrl : TriggerModifiers.None) |
            ((flags & 4) != 0 ? TriggerModifiers.Shift : TriggerModifiers.None);
    }
}
