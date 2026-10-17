using VoxPet.Core.Character;
using VoxPet.Core.Triggers;

namespace VoxPet.Core.Actions;

public sealed record ActionDefinition
{
    public string Id { get; }
    public string Name { get; }
    public int Priority { get; }
    public double DurationMs { get; }
    public string? CompatibilityTag { get; }
    public bool InterpolateTransforms { get; }
    public ActionPlayback Playback { get; }
    public IReadOnlyList<ActionTimelineEvent> Timeline { get; }
    public IReadOnlyList<TriggerDefinition> Triggers { get; }
    public ActionDefinition(string id, string name, double durationMs, IEnumerable<ActionTimelineEvent> timeline,
        int priority = 50, ActionPlayback? playback = null, string? compatibilityTag = null, IEnumerable<TriggerDefinition>? triggers = null, bool interpolateTransforms = false)
    {
        Id = DefinitionValidation.Id(id, nameof(id)); Name = DefinitionValidation.Text(name, nameof(name));
        DurationMs = DefinitionValidation.Positive(durationMs, nameof(durationMs));
        if (durationMs < 1) throw new ArgumentOutOfRangeException(nameof(durationMs), "동작 길이는1ms 이상이어야 합니다.");
        if (priority is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(priority));
        InterpolateTransforms = interpolateTransforms;
        Priority = priority; Playback = playback ?? new(); Playback.Validate();
        CompatibilityTag = compatibilityTag is null ? null : DefinitionValidation.Id(compatibilityTag, nameof(compatibilityTag));
        var events = DefinitionValidation.Copy(timeline, nameof(timeline));
        if (events.Any(e => e.TimeMs > durationMs || e.DurationMs > durationMs - e.TimeMs)) throw new ArgumentException("이벤트가 동작 길이를 벗어납니다.");
        Timeline = Array.AsReadOnly(events.OrderBy(e => e.TimeMs).ToArray());
        Triggers = new ActionBinding(id, triggers ?? []).Triggers;
    }
}
