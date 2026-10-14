using VoxPet.Core.Models;
using VoxPet.Core.Motion;

namespace VoxPet.Core.Services;

/// <summary>기존3단계 입/6DIP body/seeded blink 계약을 새 MotionEngine으로 제공하는 호환 wrapper.</summary>
public sealed class CharacterAnimator
{
    private readonly MotionEngine engine;
    public CharacterAnimator(Random? random = null) => engine = MotionEngine.Legacy(random);
    public CharacterParameters Update(double level, double seconds)
    {
        var pose = engine.Update(level, seconds); double mouth = pose.MouthOpen;
        return new(mouth, Math.Min(6, -pose.Transforms["body"].Y), 0, 0, pose.EyeOpen,
            mouth < .2 ? MouthState.Closed : mouth < .6 ? MouthState.Half : MouthState.Open);
    }
}
