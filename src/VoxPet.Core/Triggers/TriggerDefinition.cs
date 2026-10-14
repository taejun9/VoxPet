using VoxPet.Core.Character;

namespace VoxPet.Core.Triggers;

public enum TriggerKind { Hotkey, Mouse, VoiceThreshold, Interval, Manual }
[Flags]
public enum TriggerModifiers { None = 0, Ctrl = 1, Alt = 2, Shift = 4 }

public sealed record TriggerDefinition
{
    public string Id { get; }
    public string ActionId { get; }
    public TriggerKind Kind { get; }
    public string? Key { get; }
    public string? Button { get; }
    public TriggerModifiers Modifiers { get; }
    public double Threshold { get; }
    public double IntervalSeconds { get; }
    public bool StartImmediately { get; }
    public bool Enabled { get; }
    public TriggerDefinition(string id, string actionId, TriggerKind kind, string? key = null, string? button = null,
        TriggerModifiers modifiers = TriggerModifiers.None, double threshold = .5, double intervalSeconds = 5, bool startImmediately = false, bool enabled = true)
    {
        Id = DefinitionValidation.Id(id, nameof(id)); ActionId = DefinitionValidation.Id(actionId, nameof(actionId));
        if (!Enum.IsDefined(kind) || (modifiers & ~(TriggerModifiers.Ctrl | TriggerModifiers.Alt | TriggerModifiers.Shift)) != 0) throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind; Modifiers = modifiers; Enabled = enabled; StartImmediately = startImmediately;
        string? identity = key is null ? null : DefinitionValidation.Id(key, nameof(key));
        Key = kind == TriggerKind.Hotkey ? identity?.ToLowerInvariant() : identity;
        Button = button is null ? null : DefinitionValidation.Id(button, nameof(button)).ToLowerInvariant();
        Threshold = DefinitionValidation.Unit(threshold, nameof(threshold)); IntervalSeconds = DefinitionValidation.Positive(intervalSeconds, nameof(intervalSeconds));
        if (intervalSeconds is < .001 or > 86400) throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
        if (kind is TriggerKind.Hotkey or TriggerKind.Manual && Key is null) throw new ArgumentException("키/수동 트리거ID가 필요합니다.");
        if (kind == TriggerKind.Mouse && Button is not ("left" or "right" or "middle")) throw new ArgumentException("마우스 버튼을 확인하세요.");
    }
}
