using VoxPet.Core.Character;

namespace VoxPet.Core.Motion;

public sealed class BlinkMotion : IMotionSource
{
    private readonly Random random;
    private double nextBlink, blinkUntil;
    public double IntervalMin { get; }
    public double IntervalMax { get; }
    public double Duration { get; }
    public bool Smooth { get; }
    public BlinkMotion(Random? random = null, double intervalMin = 2, double intervalMax = 6, double duration = .14, bool smooth = false)
    {
        IntervalMin = VoiceMotion.Amount(intervalMin, 3600, nameof(intervalMin)); IntervalMax = VoiceMotion.Amount(intervalMax, 3600, nameof(intervalMax));
        Duration = VoiceMotion.Amount(duration, 10, nameof(duration)); Smooth = smooth;
        if (intervalMin <= 0 || intervalMax < intervalMin) throw new ArgumentOutOfRangeException(nameof(intervalMin));
        this.random = random ?? new Random(); nextBlink = NextInterval();
    }
    private double NextInterval() => IntervalMin + random.NextDouble() * (IntervalMax - IntervalMin);
    public CharacterPose Update(MotionInput input)
    {
        input.Validate();
        if (input.Seconds >= nextBlink)
        {
            blinkUntil = input.Seconds + Duration; nextBlink = blinkUntil + NextInterval();
        }
        if (input.Seconds >= blinkUntil) return new(eyeOpen: 1);
        if (!Smooth) return new(eyeOpen: 0);
        double progress = Math.Clamp((blinkUntil - input.Seconds) / Duration, 0, 1);
        // Closing (0.8..1.0) and opening (0.0..0.2) smoothstep; middle is held closed.
        double eye = progress > .8 ? (progress - .8) / .2 : progress < .2 ? (1 - progress / .2) : 0;
        return new(eyeOpen: eye * eye * (3 - 2 * eye));
    }
}
