using VoxPet.Core.Actions;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.Core.Legacy;

/// <summary>기존 간격·입력 예약 규칙은 유지하고 재생 수명은 ActionEngine에 위임한다.</summary>
public sealed class LegacyHandMotionSchedulerAdapter
{
    private readonly ActionEngine engine;
    public LegacyHandMotionSchedulerAdapter(ActionEngine? engine = null) => this.engine = engine ?? new();
    private readonly ActionDefinition[] actions = Enum.GetValues<HandGesture>().Select(g => new ActionDefinition(g.ToString(), g.ToString(), HandMotionScheduler.Duration * 1000, [])).ToArray();
    private HandMotionProfile[] profiles = [];
    private readonly double[] next = new double[5];
    private readonly bool[] pending = new bool[5];
    private double last;
    private int cursor;
    private bool stopped;
    private static void Time(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
    }
    public void Configure(HandMotionSettings settings, double seconds)
    {
        Time(seconds); settings.Validate(); seconds = Math.Max(Math.Max(last, engine.LastTime), seconds); last = seconds;
        var ordered = settings.Profiles.OrderBy(profile => profile.Gesture).ToArray();
        for (int i = 0; i < 5; i++)
        {
            var old = profiles.Length == 5 ? profiles[i] : null;
            if (old == null || old.Enabled != ordered[i].Enabled || old.Mode != ordered[i].Mode || old.IntervalSeconds != ordered[i].IntervalSeconds)
            {
                next[i] = seconds + ordered[i].IntervalSeconds; pending[i] = false;
                if (engine.Current.ActionId == ordered[i].Gesture.ToString() && old != null) engine.Stop();
            }
        }
        profiles = ordered;
    }
    public bool Trigger(HandGesture gesture, double seconds, bool preview = false)
    {
        Time(seconds); if (stopped || !Enum.IsDefined(gesture) || profiles.Length != 5) return false;
        seconds = Math.Max(Math.Max(last, engine.LastTime), seconds); last = seconds;
        var profile = profiles[(int)gesture];
        if (!preview && (!profile.Enabled || profile.Mode != HandTriggerMode.Hotkey)) return false;
        if (!engine.TryPlay(actions[(int)gesture], seconds, restart: false)) return false;
        pending[(int)gesture] = false; return true;
    }
    public HandPlayback? Update(double seconds)
    {
        Time(seconds); if (stopped || profiles.Length != 5) return null;
        seconds = Math.Max(Math.Max(last, engine.LastTime), seconds); last = seconds;
        for (int i = 0; i < 5; i++)
            if (profiles[i].Enabled && profiles[i].Mode == HandTriggerMode.Interval && seconds >= next[i])
            {
                pending[i] = true; next[i] = seconds + profiles[i].IntervalSeconds;
            }
        var state = engine.Update(seconds);
        if (!state.IsPlaying)
        {
            int chosen = -1;
            for (int n = 0; n < 5; n++) { int i = (cursor + n) % 5; if (pending[i]) { chosen = i; break; } }
            if (chosen < 0)
                for (int n = 0; n < 5; n++) { int i = (cursor + n) % 5; if (profiles[i].Enabled && profiles[i].Mode == HandTriggerMode.Always) { chosen = i; break; } }
            if (chosen >= 0) { pending[chosen] = false; engine.TryPlay(actions[chosen], seconds); state = engine.Current; cursor = (chosen + 1) % 5; }
        }
        // The same engine also owns the nine companion sample actions.
        return state.IsPlaying && Enum.TryParse<HandGesture>(state.ActionId, out var gesture) && Enum.IsDefined(gesture) ? new(gesture, state.Progress) : null;
    }
    public void Stop() { stopped = true; engine.Stop(); Array.Clear(pending); }
    public void Cancel() { engine.Stop(); Array.Clear(pending); }
}
