using System.Collections.ObjectModel;

namespace VoxPet.Core.Character;

/// <summary>모션 결과 snapshot. 부위/Anchor ID별 local 변환과 입·눈 열림을 보존한다.</summary>
public sealed record CharacterPose
{
    public double MouthOpen { get; }
    public double EyeOpen { get; }
    public IReadOnlyDictionary<string, Transform2D> Transforms { get; }

    public CharacterPose(double mouthOpen = 0, double eyeOpen = 1, IReadOnlyDictionary<string, Transform2D>? transforms = null)
    {
        MouthOpen = DefinitionValidation.Unit(mouthOpen, nameof(mouthOpen));
        EyeOpen = DefinitionValidation.Unit(eyeOpen, nameof(eyeOpen));
        var copy = new Dictionary<string, Transform2D>(StringComparer.Ordinal);
        foreach (var pair in transforms ?? new Dictionary<string, Transform2D>())
        {
            DefinitionValidation.Id(pair.Key, nameof(transforms));
            ArgumentNullException.ThrowIfNull(pair.Value, nameof(transforms));
            if (!copy.TryAdd(pair.Key, pair.Value)) throw new ArgumentException("변환ID가 중복됩니다.", nameof(transforms));
        }
        Transforms = new ReadOnlyDictionary<string, Transform2D>(copy);
    }
}
