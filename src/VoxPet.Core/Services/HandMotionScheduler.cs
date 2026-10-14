using VoxPet.Core.Legacy;
using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

public sealed record HandPlayback(HandGesture Gesture, double Progress)
{
    public int Frame => Math.Min(3, (int)(Progress * 4));
    public double Blend => Smooth(Math.Min(Progress / .12, (1 - Progress) / .12));
    private static double Smooth(double value) { value = Math.Clamp(value, 0, 1); return value * value * (3 - 2 * value); }
}

/// <summary>저장 형식과 호출 API를 보존하는 ActionEngine compatibility wrapper.</summary>
public sealed class HandMotionScheduler
{
    public const double Duration = 1.4;
    private readonly LegacyHandMotionSchedulerAdapter adapter = new();
    public void Configure(HandMotionSettings settings, double seconds) => adapter.Configure(settings, seconds);
    public bool Trigger(HandGesture gesture, double seconds, bool preview = false) => adapter.Trigger(gesture, seconds, preview);
    public HandPlayback? Update(double seconds) => adapter.Update(seconds);
    public void Stop() => adapter.Stop();
    public void Cancel() => adapter.Cancel();
}
