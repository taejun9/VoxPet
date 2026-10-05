using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

/// <summary>Blink and idle use their own clock and continue without microphone input.</summary>
public sealed class CharacterAnimator
{
    private readonly Random random;
    private double nextBlink;
    private double blinkUntil;
    public CharacterAnimator(Random? random = null)
    {
        this.random = random ?? new Random();
        nextBlink = NextInterval();
    }
    private double NextInterval() => 2 + random.NextDouble() * 4;
    public CharacterParameters Update(double level, double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        level = double.IsFinite(level) ? Math.Clamp(level, 0, 1) : 0;
        if (seconds >= nextBlink)
        {
            blinkUntil = seconds + .14;
            nextBlink = blinkUntil + NextInterval();
        }
        double idle = .5 + .5 * Math.Sin((seconds % Math.PI) * 2);
        return new(level, Math.Min(6, idle + level * 5), 0, 0, seconds < blinkUntil ? 0 : 1,
            level < .2 ? MouthState.Closed : level < .6 ? MouthState.Half : MouthState.Open);
    }
}
