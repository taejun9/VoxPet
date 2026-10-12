using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

public sealed record MotionPose(double HeadAngle, double MouthY, double LeftAngle, double RightAngle, double HandX, double HandY, double HandScale, HandGesture? Gesture, double Effect)
{
    public static MotionPose Rest { get; } = new(0, 0, 0, 0, 0, 0, 1, null, 0);
}
/// <summary>캐릭터 조각과 소매/장식에 같은 관절값을 전달한다. 음성 데이터나 PNG를 읽지 않는다.</summary>
public static class CharacterMotion
{
    public static MotionPose Evaluate(CharacterRig rig, CharacterParameters state, double seconds, HandPlayback? hand, HandMotionProfile? profile)
    {
        rig.Validate();
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        double head = rig.HeadMotion ? 2 * Math.Sin(seconds % (Math.PI * 4) / 2) + Finite(state.HeadTilt, -8, 8) : 0;
        double mouth = Finite(state.MouthOpen, 0, 1) * rig.MouthTravel;
        if (hand == null || profile == null) return MotionPose.Rest with { HeadAngle = head, MouthY = mouth };
        profile.Validate();
        if (!double.IsFinite(hand.Progress) || hand.Progress is < 0 or > 1 || hand.Gesture != profile.Gesture) throw new ArgumentException("동작 진행값을 확인하세요.");
        double envelope = hand.Blend, wave = Math.Sin(hand.Progress * Math.PI * 4);
        var (left, right) = hand.Gesture switch
        {
            HandGesture.Claw => (-18 + wave * 9, 18 - wave * 9),
            HandGesture.ToyGun => (-6d, 26 + wave * 4),
            HandGesture.Heart => (-30d, 30d),
            HandGesture.ToySword => (-5d, 24 + wave * 12),
            _ => (-8d, 22 + wave * 10)
        };
        return new(head, mouth, rig.Left.Enabled ? left * envelope : 0, rig.Right.Enabled ? right * envelope : 0,
            profile.X * envelope, profile.Y * envelope, 1 + (profile.Scale - 1) * envelope, hand.Gesture, envelope);
    }
    private static double Finite(double value, double min, double max) => double.IsFinite(value) ? Math.Clamp(value, min, max) : 0;
}
