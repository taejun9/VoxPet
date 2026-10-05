namespace VoxPet.Core.Models;

/// <summary>Immutable, validated settings snapshot; durations are milliseconds.</summary>
public sealed record AudioSettings(double NoiseGate = -50, double NormalizeMin = -50,
    double NormalizeMax = -10, double Sensitivity = 1, double AttackMs = 40, double ReleaseMs = 140)
{
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
