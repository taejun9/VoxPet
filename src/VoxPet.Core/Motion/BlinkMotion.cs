using VoxPet.Core.Character;

namespace VoxPet.Core.Motion;

public sealed class BlinkMotion : IMotionSource
{
    private readonly Random random;
    private double nextBlink, blinkUntil;
    public double IntervalMin { get; }
    public double IntervalMax { get; }
    public double Duration { get; }
    public BlinkMotion(Random? random = null, double intervalMin = 2, double intervalMax = 6, double duration = .14)
    {
        IntervalMin = VoiceMotion.Amount(intervalMin, 3600, nameof(intervalMin)); IntervalMax = VoiceMotion.Amount(intervalMax, 3600, nameof(intervalMax));
        Duration = VoiceMotion.Amount(duration, 10, nameof(duration));
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
        return new(eyeOpen: input.Seconds < blinkUntil ? 0 : 1);
    }
}
