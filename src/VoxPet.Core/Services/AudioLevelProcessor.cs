using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

/// <summary>
/// Raw는 정규화만 적용, Target은 Gate·감도 적용, VoiceLevel은 시간 평활화까지 적용한 값이다.
/// </summary>
public sealed record ProcessedLevel(double Raw, double Target, double VoiceLevel);

/// <summary>단일 소비자 스레드가 소유하는 시간 기반 Attack/Release envelope. 최종 레벨은 유한한 0~1이다.</summary>
public sealed class AudioLevelProcessor
{
    public double VoiceLevel { get; private set; }
    /// <summary>
    /// elapsedSeconds는 이전 갱신부터의 단조 증가 시계 경과 초다.
    /// UI 프레임 수 대신 실제 시간을 사용해 타이머 주기에 따른 반응 속도 차이를 줄인다.
    /// </summary>
    public ProcessedLevel Update(double dbfs, double elapsedSeconds, AudioSettings settings)
    {
        settings.Validate();
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        dbfs = double.IsFinite(dbfs) ? dbfs : -120;
        // RAW는 Gate 이전 값이다. Gate는 원본 dBFS와 비교하므로 감도가 Gate를 우회하지 못한다.
        double raw = Math.Clamp((dbfs - settings.NormalizeMin) / (settings.NormalizeMax - settings.NormalizeMin), 0, 1);
        double target = dbfs < settings.NoiseGate ? 0 : Math.Clamp(raw * settings.Sensitivity, 0, 1);
        // 증가에는 Attack, 감소에는 Release를 사용하고 ms를 초로 바꾼다.
        // alpha = 1-exp(-dt/tau): dt=0은 유지, tau=0은 다음 유효 갱신에서 즉시 목표값.
        double tau = (target > VoiceLevel ? settings.AttackMs : settings.ReleaseMs) / 1000;
        double alpha = elapsedSeconds == 0 ? 0 : tau == 0 ? 1 : 1 - Math.Exp(-elapsedSeconds / tau);
        VoiceLevel = Math.Clamp(VoiceLevel + alpha * (target - VoiceLevel), 0, 1);
        // 지수 감소의 작은 잔여를 0에 정착시켜 무음에서 입이 미세하게 반응하지 않게 한다.
        if (target == 0 && VoiceLevel < .001) VoiceLevel = 0;
        return new(raw, target, VoiceLevel);
    }
    /// <summary>
    /// Stop·음소거·세션 변경 시 이전 envelope를 즉시 버린다.
    /// </summary>
    public void Reset() => VoiceLevel = 0;
}
