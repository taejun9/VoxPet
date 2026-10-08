namespace VoxPet.Core.Services;

/// <summary>
/// 조용히 있는 동안의 dBFS 숫자만 임시 수집한다. 음성/소음 분류기가 아니며 PCM은 보관하지 않는다.
/// 중복·오래된 표본을 제외하고 90백분위수에 6dB 여유를 둔 Gate를 추천한다.
/// </summary>
public sealed class NoiseGateCalibration
{
    private readonly List<double> samples = [];
    private double firstTime, lastTime = double.NegativeInfinity;
    public int Count => samples.Count;
    public void AddSample(double dbfs, double seconds)
    {
        if (!double.IsFinite(dbfs) || dbfs is < -120 or > 0 || !double.IsFinite(seconds) ||
            seconds <= lastTime || samples.Count >= 512) return;
        if (samples.Count == 0) firstTime = seconds;
        lastTime = seconds; samples.Add(dbfs);
    }
    /// <summary>2초 이상에 걸친 30개 이상의 실제 표본이 필요하다. 무신호·너무 큰 주변 소리는 적용하지 않는다.</summary>
    public bool TryRecommend(out double gate)
    {
        gate = 0;
        if (samples.Count < 30 || lastTime - firstTime < 2) return false;
        var sorted = samples.Order().ToArray();
        double floor = sorted[(int)Math.Ceiling(sorted.Length * .9) - 1];
        if (floor <= -119 || floor > -21) return false;
        gate = Math.Clamp(Math.Ceiling(floor + 6), -80, -15);
        return true;
    }
}
