namespace VoxPet.Core.Character;

/// <summary>분리 부위의 어깨 경계는 고정하고 손목까지 관절 영향을 부드럽게 늘린다.</summary>
public sealed record PartSkinning
{
    public double RootY { get; }
    public double FullY { get; }
    public string? EndAnchor { get; }
    public string? RootAnchor { get; }
    public PartSkinning(double rootY, double fullY, string? endAnchor = null, string? rootAnchor = null)
    {
        RootAnchor = rootAnchor == null ? null : DefinitionValidation.Id(rootAnchor, nameof(rootAnchor));
        EndAnchor = endAnchor == null ? null : DefinitionValidation.Id(endAnchor, nameof(endAnchor));
        RootY = DefinitionValidation.Finite(rootY, nameof(rootY)); FullY = DefinitionValidation.Finite(fullY, nameof(fullY));
        if (rootY < 0 || fullY - rootY < 1 || fullY > 16384) throw new ArgumentOutOfRangeException(nameof(fullY));
    }
}
