using VoxPet.Core.Character;

namespace VoxPet.Core.Actions;

/// <summary>한 번에 한 action. 낮은 priority는 거부하며 지연 tick은 현재 loop 상태만 평가한다.</summary>
public sealed class ActionEngine
{
    private readonly IReadOnlyDictionary<string, PoseDefinition> poses;
    private ActionDefinition? active;
    private double started, lastTime;
    public ActionState Current { get; private set; } = ActionState.Idle;
    public ActionDefinition? Definition => active;
    public ActionEngine(IEnumerable<PoseDefinition>? poses = null)
    {
        var copy = DefinitionValidation.Copy(poses ?? [], nameof(poses));
        if (copy.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != copy.Count) throw new ArgumentException("포즈ID 중복입니다.");
        this.poses = copy.ToDictionary(p => p.Id, StringComparer.Ordinal);
    }
    private void Time(double seconds)
    {
        DefinitionValidation.Finite(seconds, nameof(seconds));
        if (seconds < lastTime || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds), "단조 증가 시간을 사용하세요.");
        lastTime = seconds;
    }
    public bool TryPlay(ActionDefinition action, double seconds, bool restart = true)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action.Timeline.Any(e => e.Kind == ActionEventKind.Pose && e.Value != "idle" && !poses.ContainsKey(e.Value!))) throw new ArgumentException("액션 포즈 참조가 없습니다.", nameof(action));
        var current = Update(seconds);
        if (current.IsPlaying && active is not null)
        {
            if (action.Priority < active.Priority || !active.Playback.Interruptible && action.Priority < ActionPriority.System) return false;
            if (!restart && action.Id == active.Id) return false;
        }
        active = action; started = seconds; Current = ActionTimeline.Evaluate(action, poses, 0, true); return true;
    }
    public ActionState Update(double seconds)
    {
        Time(seconds); if (active is null) return Current = ActionState.Idle;
        double elapsed = seconds - started, duration = active.DurationMs / 1000;
        if (active.Playback.Mode == ActionPlaybackMode.Once && elapsed >= duration)
        {
            if (active.Playback.ReturnToIdle) { active = null; return Current = ActionState.Idle; }
            return Current = ActionTimeline.Evaluate(active, poses, active.DurationMs, false);
        }
        double milliseconds = active.Playback.Mode == ActionPlaybackMode.Loop ? (elapsed % duration) * 1000 : elapsed * 1000;
        return Current = ActionTimeline.Evaluate(active, poses, milliseconds, true);
    }
    public void Stop() { active = null; Current = ActionState.Idle; }
}
