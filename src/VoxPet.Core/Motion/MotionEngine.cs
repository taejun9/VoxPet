using VoxPet.Core.Character;

namespace VoxPet.Core.Motion;

/// <summary>caller time만 사용하는 모션 합성기. 입은 max, 눈은 min, local 이동/회전은 합산한다.</summary>
public sealed class MotionEngine
{
    private readonly IReadOnlyList<IMotionSource> sources;
    public MotionEngine(IEnumerable<IMotionSource> sources) => this.sources = DefinitionValidation.Copy(sources, nameof(sources));
    public static MotionEngine Legacy(Random? random = null) => new([new VoiceMotion(), new IdleMotion(1, 0), new BlinkMotion(random)]);
    public CharacterPose Update(double level, double seconds, double deltaTime = 0) => Update(new MotionInput(seconds, deltaTime, double.IsFinite(level) ? Math.Clamp(level, 0, 1) : 0));
    public CharacterPose Update(MotionInput input)
    {
        input.Validate(); var transforms = new Dictionary<string, Transform2D>(StringComparer.Ordinal); double mouth = 0, eyes = 1;
        foreach (var source in sources)
        {
            var pose = source.Update(input) ?? throw new InvalidOperationException("모션 소스가 pose를 반환하지 않았습니다.");
            mouth = Math.Max(mouth, pose.MouthOpen); eyes = Math.Min(eyes, pose.EyeOpen);
            foreach (var pair in pose.Transforms)
            {
                var old = transforms.GetValueOrDefault(pair.Key) ?? Transform2D.Identity; var value = pair.Value;
                transforms[pair.Key] = new(old.X + value.X, old.Y + value.Y, old.ScaleX * value.ScaleX, old.ScaleY * value.ScaleY, old.Rotation + value.Rotation);
            }
        }
        return new(mouth, eyes, transforms);
    }
}
