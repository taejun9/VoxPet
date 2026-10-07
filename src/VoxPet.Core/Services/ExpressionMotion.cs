namespace VoxPet.Core.Services;

/// <summary>누적 monotonic 초로 계산하는 부드러운 표정 전환과 눈물. 오디오 입력에 의존하지 않는다.</summary>
public static class ExpressionMotion
{
    public const double TransitionSeconds = .24;
    public static double Blend(double elapsed)
    {
        if (!double.IsFinite(elapsed)) return elapsed > 0 ? 1 : 0;
        double t = Math.Clamp(elapsed / TransitionSeconds, 0, 1);
        return t * t * (3 - 2 * t);
    }
    public static (double Offset, double Opacity) Tear(double seconds, double phase = 0)
    {
        if (!double.IsFinite(seconds) || !double.IsFinite(phase) || seconds < 0) return (0, 0);
        double t = ((seconds % 1.4) / 1.4 + phase) % 1;
        if (t < 0) t += 1;
        return (t * 70, Math.Sin(t * Math.PI));
    }
}
