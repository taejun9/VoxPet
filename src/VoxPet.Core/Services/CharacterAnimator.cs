using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

/// <summary>음성과 독립적인 blink/idle 시간축을 사용해 마이크가 꺼져도 캐릭터가 대기 동작을 한다.</summary>
public sealed class CharacterAnimator
{
    private readonly Random random;
    private double nextBlink;
    private double blinkUntil;
    /// <summary>
    /// 테스트에서 고정 seed의 Random을 주입하면 깜빡임 시퀀스를 재현할 수 있다.
    /// </summary>
    public CharacterAnimator(Random? random = null)
    {
        this.random = random ?? new Random();
        nextBlink = NextInterval();
    }
    // 깜빡임 사이의 대기 시간을 2~6초에서 선택한다.
    private double NextInterval() => 2 + random.NextDouble() * 4;
    /// <summary>
    /// seconds는 호출자가 일관된 시간축으로 제공하는 누적 초, level은 입 반응값이다.
    /// 입은 0.2/0.6 경계로 고르고 몸 이동량은 6 DIP 이하로 제한한다.
    /// </summary>
    public CharacterParameters Update(double level, double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        level = double.IsFinite(level) ? Math.Clamp(level, 0, 1) : 0;
        if (seconds >= nextBlink)
        {
            // 눈 감음은 140ms 유지하고 다음 간격은 눈을 다시 뜨는 시점부터 계산한다.
            blinkUntil = seconds + .14;
            nextBlink = blinkUntil + NextInterval();
        }
        // 입력 없이도 0~1 DIP의 주기적 움직임. 큰 누적 시간은 sin 계산 전에 주기로 줄인다.
        double idle = .5 + .5 * Math.Sin((seconds % Math.PI) * 2);
        return new(level, Math.Min(6, idle + level * 5), 0, 0, seconds < blinkUntil ? 0 : 1,
            level < .2 ? MouthState.Closed : level < .6 ? MouthState.Half : MouthState.Open);
    }
}
