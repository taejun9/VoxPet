using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

public sealed record ProcessedLevel(double Raw, double Target, double VoiceLevel);

/// <summary>Time-based attack/release envelope; owned by one consumer thread.</summary>
public sealed class AudioLevelProcessor
{
    public double VoiceLevel { get; private set; }
    public ProcessedLevel Update(double dbfs, double elapsedSeconds, AudioSettings settings)
    {
        settings.Validate();
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        dbfs = double.IsFinite(dbfs) ? dbfs : -120;
        double raw = Math.Clamp((dbfs - settings.NormalizeMin) / (settings.NormalizeMax - settings.NormalizeMin), 0, 1);
        double target = dbfs < settings.NoiseGate ? 0 : Math.Clamp(raw * settings.Sensitivity, 0, 1);
        double tau = (target > VoiceLevel ? settings.AttackMs : settings.ReleaseMs) / 1000;
        double alpha = elapsedSeconds == 0 ? 0 : tau == 0 ? 1 : 1 - Math.Exp(-elapsedSeconds / tau);
        VoiceLevel = Math.Clamp(VoiceLevel + alpha * (target - VoiceLevel), 0, 1);
        if (target == 0 && VoiceLevel < .001) VoiceLevel = 0;
        return new(raw, target, VoiceLevel);
    }
    public void Reset() => VoiceLevel = 0;
}
