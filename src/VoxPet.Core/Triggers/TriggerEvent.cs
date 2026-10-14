using VoxPet.Core.Character;

namespace VoxPet.Core.Triggers;

/// <summary>키 내용/좌표 없이 지정한 입력 identity만 전달한다. pulse는 key-up 없는 provider 통지다.</summary>
public sealed record TriggerEvent
{
    public TriggerKind Kind { get; }
    public string? Key { get; }
    public string? Button { get; }
    public TriggerModifiers Modifiers { get; }
    public bool Pressed { get; }
    public bool IsPulse { get; }
    public TriggerEvent(TriggerKind kind, string? key = null, string? button = null, TriggerModifiers modifiers = TriggerModifiers.None,
        bool pressed = true, bool isPulse = false)
    {
        if (!Enum.IsDefined(kind) || (modifiers & ~(TriggerModifiers.Ctrl | TriggerModifiers.Alt | TriggerModifiers.Shift)) != 0) throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind; Modifiers = modifiers; Pressed = pressed; IsPulse = isPulse;
        string? identity = key is null ? null : DefinitionValidation.Id(key, nameof(key));
        Key = kind == TriggerKind.Hotkey ? identity?.ToLowerInvariant() : identity;
        Button = button is null ? null : DefinitionValidation.Id(button, nameof(button)).ToLowerInvariant();
        if (kind is TriggerKind.Hotkey or TriggerKind.Manual && Key is null) throw new ArgumentException("입력ID가 필요합니다.");
        if (kind == TriggerKind.Mouse && Button is not ("left" or "right" or "middle")) throw new ArgumentException("마우스 버튼을 확인하세요.");
    }
}
