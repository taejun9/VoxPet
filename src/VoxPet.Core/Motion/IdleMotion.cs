using VoxPet.Core.Character;

namespace VoxPet.Core.Motion;

public sealed class IdleMotion : IMotionSource
{
    public double Breathing { get; }
    public double Sway { get; }
    public double Squash { get; }
    public IdleMotion(double breathing = .4, double sway = .2, double squash = 0)
    {
        Breathing = VoiceMotion.Amount(breathing, 64, nameof(breathing)); Sway = VoiceMotion.Amount(sway, 64, nameof(sway));
        Squash = VoiceMotion.Amount(squash, 1, nameof(squash));
    }
    public CharacterPose Update(MotionInput input)
    {
        input.Validate();
        // 큰 누적 시간을 주기로 감소시킨 뒤 삼각 함수를 적용한다.
        double breath = .5 + .5 * Math.Sin((input.Seconds % Math.PI) * 2);
        double sway = Math.Sin((input.Seconds % (Math.PI * 4)) / 2);
        double sx = Squash > 0 ? 1 - Squash * (breath - .5) : 1;
        double sy = Squash > 0 ? 1 + Squash * (breath - .5) : 1;
        return new(transforms: new Dictionary<string, Transform2D> { ["body"] = new(Sway * sway, -Breathing * breath, sx, sy) });
    }
}
