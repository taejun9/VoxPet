using VoxPet.Core.Character;

namespace VoxPet.Core.Attachments;

public sealed record AttachmentDefinition
{
    public string Id { get; }
    public string Name { get; }
    public string Category { get; }
    public string? CompatibilityTag { get; }
    public string? ExclusiveGroup { get; }
    public IReadOnlyList<AttachmentLayer> Layers { get; }
    public AttachmentDefinition(string id, string name, string category, IEnumerable<AttachmentLayer> layers,
        string? compatibilityTag = null, string? exclusiveGroup = null)
    {
        Id = DefinitionValidation.Id(id, nameof(id)); Name = DefinitionValidation.Text(name, nameof(name));
        Category = DefinitionValidation.Id(category, nameof(category));
        CompatibilityTag = compatibilityTag is null ? null : DefinitionValidation.Id(compatibilityTag, nameof(compatibilityTag));
        ExclusiveGroup = exclusiveGroup is null ? null : DefinitionValidation.Id(exclusiveGroup, nameof(exclusiveGroup));
        Layers = DefinitionValidation.Copy(layers, nameof(layers));
        if (Layers.Count == 0 || Layers.Select(l => l.Id).Distinct(StringComparer.Ordinal).Count() != Layers.Count) throw new ArgumentException("부착물 레이어ID를 확인하세요.");
    }
}
