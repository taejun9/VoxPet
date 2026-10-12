using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

public sealed record HandPlayback(HandGesture Gesture, double Progress)
{
    public int Frame => Math.Min(3, (int)(Progress * 4));
    public double Blend => Smooth(Math.Min(Progress / .12, (1 - Progress) / .12));
    private static double Smooth(double value) { value = Math.Clamp(value, 0, 1); return value * value * (3 - 2 * value); }
}

/// <summary>한 번에 한 동작. 간격 요청은 동작별 하나만 예약하고 지연된 프레임에서 밀린 횟수를 재생하지 않는다.</summary>
public sealed class HandMotionScheduler
{
    public const double Duration = 1.4;
    private HandMotionProfile[] profiles = [];
    private readonly double[] next = new double[5];
    private readonly bool[] pending = new bool[5];
    private HandGesture? active;
    private double started, last;
    private int cursor;
    private bool stopped;
    private static void Time(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
    }
    public void Configure(HandMotionSettings settings, double seconds)
    {
        Time(seconds); settings.Validate(); seconds = Math.Max(last, seconds); last = seconds;
        var ordered = settings.Profiles.OrderBy(profile => profile.Gesture).ToArray();
        for (int i = 0; i < 5; i++)
        {
            var old = profiles.Length == 5 ? profiles[i] : null;
            if (old == null || old.Enabled != ordered[i].Enabled || old.Mode != ordered[i].Mode || old.IntervalSeconds != ordered[i].IntervalSeconds)
            {
                next[i] = seconds + ordered[i].IntervalSeconds; pending[i] = false;
                if (active == ordered[i].Gesture && old != null) active = null;
            }
        }
        profiles = ordered;
    }
    public bool Trigger(HandGesture gesture, double seconds, bool preview = false)
    {
        Time(seconds); if (stopped || !Enum.IsDefined(gesture) || profiles.Length != 5) return false;
        seconds = Math.Max(last, seconds); last = seconds;
        var profile = profiles[(int)gesture];
        if (!preview && (!profile.Enabled || profile.Mode != HandTriggerMode.Hotkey)) return false;
        // 같은 WM_HOTKEY의 연속 통지는 시작을 반복하지 않는다. 다른 동작의 명시 입력은 즉시 우선한다.
        if (active == gesture && seconds - started < Duration) return false;
        active = gesture; started = seconds; pending[(int)gesture] = false; return true;
    }
    public HandPlayback? Update(double seconds)
    {
        Time(seconds); if (stopped || profiles.Length != 5) return null;
        seconds = Math.Max(last, seconds); last = seconds;
        for (int i = 0; i < 5; i++)
            if (profiles[i].Enabled && profiles[i].Mode == HandTriggerMode.Interval && seconds >= next[i])
            {
                pending[i] = true; next[i] = seconds + profiles[i].IntervalSeconds;
            }
        if (active != null && seconds - started >= Duration) active = null;
        if (active == null)
        {
            int chosen = -1;
            for (int n = 0; n < 5; n++) { int i = (cursor + n) % 5; if (pending[i]) { chosen = i; break; } }
            if (chosen < 0)
                for (int n = 0; n < 5; n++) { int i = (cursor + n) % 5; if (profiles[i].Enabled && profiles[i].Mode == HandTriggerMode.Always) { chosen = i; break; } }
            if (chosen >= 0) { pending[chosen] = false; active = (HandGesture)chosen; started = seconds; cursor = (chosen + 1) % 5; }
        }
        return active is { } gesture ? new(gesture, Math.Clamp((seconds - started) / Duration, 0, 1)) : null;
    }
    public void Stop() { stopped = true; active = null; Array.Clear(pending); }
    public void Cancel() { active = null; Array.Clear(pending); }
}
