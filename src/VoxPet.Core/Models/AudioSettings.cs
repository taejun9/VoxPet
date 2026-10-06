namespace VoxPet.Core.Models;

/// <summary>
/// UI가 with 식으로 교체하는 불변 오디오 설정. Gate/정규화 경계는 dBFS,
/// Sensitivity는 정규화 레벨의 배율, AttackMs/ReleaseMs는 밀리초 시정수다.
/// 생성만으로 검증하지 않으므로 사용·저장 경계에서 Validate를 호출한다.
/// </summary>
public sealed record AudioSettings(double NoiseGate = -50, double NormalizeMin = -50,
    double NormalizeMax = -10, double Sensitivity = 1, double AttackMs = 40, double ReleaseMs = 140)
{
    /// <summary>
    /// NaN/Infinity와 역전된 정규화 구간을 거부한다. 이 범위는 Core 계약이며 UI 슬라이더보다 넓다.
    /// </summary>
    public void Validate()
    {
        if (!double.IsFinite(NoiseGate) || NoiseGate is < -120 or > 0 ||
            !double.IsFinite(NormalizeMin) || NormalizeMin is < -120 or > 0 ||
            !double.IsFinite(NormalizeMax) || NormalizeMax is < -120 or > 0 || NormalizeMin >= NormalizeMax ||
            !double.IsFinite(Sensitivity) || Sensitivity is < 0 or > 10 ||
            !double.IsFinite(AttackMs) || AttackMs is < 0 or > 5000 ||
            !double.IsFinite(ReleaseMs) || ReleaseMs is < 0 or > 5000)
            throw new ArgumentOutOfRangeException(nameof(AudioSettings), "오디오 설정 범위를 확인하세요.");
    }
}
