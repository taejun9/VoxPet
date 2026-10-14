using System.Collections.ObjectModel;

namespace VoxPet.Core.Character;

/// <summary>이름 있는 포즈 snapshot. null sprite는 숨김이며 매 포즈는 기본 상태에서 적용한다.</summary>
public sealed record PoseDefinition
{
    public string Id { get; }
    public string Name { get; }
    public IReadOnlyDictionary<string, string?> Sprites { get; }
    public IReadOnlyDictionary<string, Transform2D> Transforms { get; }
    public PoseDefinition(string id, string? name = null, IReadOnlyDictionary<string, string?>? sprites = null, IReadOnlyDictionary<string, Transform2D>? transforms = null)
    {
        Id = DefinitionValidation.Id(id, nameof(id)); Name = DefinitionValidation.Id(name ?? id, nameof(name));
        var copy = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var pair in sprites ?? new Dictionary<string, string?>())
        {
            DefinitionValidation.Id(pair.Key, nameof(sprites));
            if (pair.Value is not null) DefinitionValidation.Asset(pair.Value, ".png", nameof(sprites));
            copy.Add(pair.Key, pair.Value);
        }
        Sprites = new ReadOnlyDictionary<string, string?>(copy);
        Transforms = new CharacterPose(transforms: transforms).Transforms;
    }
}
