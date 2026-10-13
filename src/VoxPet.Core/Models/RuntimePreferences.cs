namespace VoxPet.Core.Models;

public enum RuntimeOperation { Play, NextAction, PreviousAction, NextExpression, PreviousExpression, NextOutfit, PreviousOutfit, Reset }
public sealed record RuntimeBinding(RuntimeOperation Operation, uint Key, uint Modifiers)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Operation) || Key is not (>= 0x30 and <= 0x5a or >= 0x70 and <= 0x7b or 0x20) || Key is > 0x39 and < 0x41 || Modifiers > 7 ||
            Modifiers == 6 && (Key == 0x4d || Key is >= 0x70 and <= 0x7b)) throw new ArgumentException("문자·숫자·F키·Space를 사용하세요. 기존 음소거·표정 키는 예약되어 있습니다.");
    }
    public string Label => (Modifiers.HasFlagValue(2) ? "Ctrl+" : "") + (Modifiers.HasFlagValue(1) ? "Alt+" : "") + (Modifiers.HasFlagValue(4) ? "Shift+" : "") +
        (Key == 0x20 ? "Space" : Key >= 0x70 ? "F" + (Key - 0x6f) : ((char)Key).ToString());
}
internal static class ModifierBits { internal static bool HasFlagValue(this uint value, uint flag) => (value & flag) != 0; }
public sealed record RuntimeSelection(string PackId, int Action = 0, int Outfit = -1, int Weapon = -1, bool Enabled = true);
public sealed record RuntimePreferences(Dictionary<string, RuntimeSelection> Characters, RuntimeBinding[] Bindings, bool LeftClick = false)
{
    public static RuntimePreferences Default() => new([], [], false);
    public void Validate()
    {
        if (Characters == null || Bindings == null || Characters.Count > 2 || Bindings.Length > 8) throw new ArgumentException("런타임 설정 목록을 확인하세요.");
        foreach (var pair in Characters)
            if (!CharacterCatalog.All.Any(c => c.Id == pair.Key) || pair.Value == null || pair.Value.PackId == null || pair.Value.PackId.Length != 32 || !pair.Value.PackId.All(char.IsAsciiHexDigit) || pair.Value.Action is < 0 or > 15 || pair.Value.Outfit is < -1 or > 15 || pair.Value.Weapon is < -1 or > 15) throw new ArgumentException("등록 세트 선택을 확인하세요.");
        foreach (var binding in Bindings) { if (binding == null) throw new ArgumentException("키 설정을 확인하세요."); binding.Validate(); }
        if (Bindings.Select(b => b.Operation).Distinct().Count() != Bindings.Length || Bindings.Select(b => (b.Key, b.Modifiers)).Distinct().Count() != Bindings.Length) throw new ArgumentException("키 또는 기능이 중복되었습니다.");
    }
}
