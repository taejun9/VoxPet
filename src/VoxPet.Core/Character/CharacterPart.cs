namespace VoxPet.Core.Character;

/// <summary>새 신체 부위가 enum 변경 없이 추가될 수 있는 PNG 파츠 정의.</summary>
public sealed record CharacterPart
{
    public string Id { get; }
    public string Type { get; }
    public string Asset { get; }
    public string? Anchor { get; }
    public string? Side { get; }
    public int ZIndex { get; }
    public Transform2D Transform { get; }

    public CharacterPart(string id, string type, string asset, string? anchor = null, int zIndex = 0,
        Transform2D? transform = null, string? side = null)
    {
        Id = DefinitionValidation.Id(id, nameof(id));
        Type = DefinitionValidation.Id(type, nameof(type));
        Asset = DefinitionValidation.Asset(asset, ".png", nameof(asset));
        Anchor = anchor is null ? null : DefinitionValidation.Id(anchor, nameof(anchor));
        Side = side is null ? null : DefinitionValidation.Id(side, nameof(side));
        ZIndex = zIndex;
        Transform = transform ?? Transform2D.Identity;
    }
}
