using VoxPet.Core.Character;

namespace VoxPet.Core.Actions;

public enum ActionEventKind { Pose, Transform, Visibility, Sprite, Effect, Sound, Expression, Attachment }

/// <summary>플랫폼 독립 timeline 이벤트. time/duration은 ms이며 Sound는 규격 예약이다.</summary>
public sealed record ActionTimelineEvent
{
    public double TimeMs { get; }
    public ActionEventKind Kind { get; }
    public string? Target { get; }
    public string? Value { get; }
    public Transform2D? Transform { get; }
    public int ZIndex { get; }
    public string? EndAnchor { get; }
    public Transform2D EndTransform { get; }
    public bool Enabled { get; }
    public double DurationMs { get; }
    public ActionTimelineEvent(double timeMs, ActionEventKind kind, string? target = null, string? value = null,
        Transform2D? transform = null, bool enabled = true, double durationMs = 0, string? endAnchor = null, Transform2D? endTransform = null, int zIndex = 2000)
    {
        TimeMs = DefinitionValidation.Finite(timeMs, nameof(timeMs)); DurationMs = DefinitionValidation.Finite(durationMs, nameof(durationMs));
        if (timeMs < 0 || durationMs < 0 || !Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(timeMs));
        ZIndex = zIndex;
        if (zIndex != 2000 && kind != ActionEventKind.Effect) throw new ArgumentException("효과의 순서만 지정합니다.");
        EndAnchor = endAnchor == null ? null : DefinitionValidation.Id(endAnchor, nameof(endAnchor)); EndTransform = endTransform ?? Transform2D.Identity;
        if (endAnchor != null && kind != ActionEventKind.Effect) throw new ArgumentException("연결 끝점은 effect에만 지정합니다.");
        Kind = kind; Target = target is null ? null : DefinitionValidation.Id(target, nameof(target));
        Value = value; Transform = transform; Enabled = enabled;
        switch (kind)
        {
            case ActionEventKind.Pose or ActionEventKind.Expression or ActionEventKind.Attachment:
                DefinitionValidation.Id(value!, nameof(value)); break;
            case ActionEventKind.Transform:
                DefinitionValidation.Id(target!, nameof(target)); ArgumentNullException.ThrowIfNull(transform); break;
            case ActionEventKind.Visibility:
                DefinitionValidation.Id(target!, nameof(target)); break;
            case ActionEventKind.Sprite:
                DefinitionValidation.Id(target!, nameof(target)); DefinitionValidation.Asset(value!, ".png", nameof(value)); break;
            case ActionEventKind.Effect:
                DefinitionValidation.Id(target!, nameof(target)); DefinitionValidation.Asset(value!, ".png", nameof(value));
                DefinitionValidation.Positive(durationMs, nameof(durationMs)); break;
            case ActionEventKind.Sound:
                DefinitionValidation.Asset(value!, ".wav", nameof(value)); break;
        }
    }
}
