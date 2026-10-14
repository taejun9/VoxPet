using VoxPet.Core.Character;

namespace VoxPet.Core.Actions;

public sealed record ActionDefinition
{
    public string Id { get; }
    public string Name { get; }
    public int Priority { get; }
    public double DurationMs { get; }
    public string? CompatibilityTag { get; }
    public ActionPlayback Playback { get; }
    public IReadOnlyList<ActionTimelineEvent> Timeline { get; }
    public ActionDefinition(string id, string name, double durationMs, IEnumerable<ActionTimelineEvent> timeline,
        int priority = 50, ActionPlayback? playback = null, string? compatibilityTag = null)
    {
        Id = DefinitionValidation.Id(id, nameof(id)); Name = DefinitionValidation.Text(name, nameof(name));
        DurationMs = DefinitionValidation.Positive(durationMs, nameof(durationMs));
        if (priority is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(priority));
        Priority = priority; Playback = playback ?? new(); Playback.Validate();
        CompatibilityTag = compatibilityTag is null ? null : DefinitionValidation.Id(compatibilityTag, nameof(compatibilityTag));
        var events = DefinitionValidation.Copy(timeline, nameof(timeline));
        if (events.Any(e => e.TimeMs >= durationMs || e.DurationMs > durationMs - e.TimeMs)) throw new ArgumentException("이벤트가 동작 길이를 벗어납니다.");
        Timeline = Array.AsReadOnly(events.OrderBy(e => e.TimeMs).ToArray());
    }
}
