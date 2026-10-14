using VoxPet.Core.Character;

namespace VoxPet.Core.Motion;

public interface IMotionSource
{
    CharacterPose Update(MotionInput input);
}

public readonly record struct MotionInput(double Seconds, double DeltaTime, double VoiceLevel)
{
    public void Validate()
    {
        DefinitionValidation.Finite(Seconds, nameof(Seconds)); DefinitionValidation.Finite(DeltaTime, nameof(DeltaTime));
        DefinitionValidation.Unit(VoiceLevel, nameof(VoiceLevel));
        if (Seconds < 0 || DeltaTime < 0) throw new ArgumentOutOfRangeException(nameof(Seconds));
    }
}
