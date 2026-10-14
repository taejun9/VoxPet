using VoxPet.Core.Character;

namespace VoxPet.Core.Motion;

public sealed class VoiceMotion : IMotionSource
{
    public double Mouth { get; }
    public double BodyBounce { get; }
    public double HeadBounce { get; }
    public double HeadRotation { get; }
    public VoiceMotion(double mouth = 1, double bodyBounce = 5, double headBounce = 0, double headRotation = 0)
    {
        Mouth = Amount(mouth, 16, nameof(mouth)); BodyBounce = Amount(bodyBounce, 512, nameof(bodyBounce));
        HeadBounce = Amount(headBounce, 512, nameof(headBounce)); HeadRotation = Amount(headRotation, 360, nameof(headRotation));
    }
    internal static double Amount(double value, double maximum, string parameter)
    {
        DefinitionValidation.Finite(value, parameter);
        if (value < 0 || value > maximum) throw new ArgumentOutOfRangeException(parameter);
        return value;
    }
    public CharacterPose Update(MotionInput input)
    {
        input.Validate();
        return new(Math.Clamp(input.VoiceLevel * Mouth, 0, 1), transforms: new Dictionary<string, Transform2D>
        {
            ["body"] = new(y: -input.VoiceLevel * BodyBounce),
            ["head"] = new(y: -input.VoiceLevel * HeadBounce, rotation: input.VoiceLevel * HeadRotation)
        });
    }
}
