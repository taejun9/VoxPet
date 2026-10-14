namespace VoxPet.Core.Character;

public sealed record FaceDefinition
{
    public string EyesAnchor { get; }
    public string BrowsAnchor { get; }
    public int EyesZIndex { get; }
    public int BrowsZIndex { get; }
    public string? RequiredVisiblePart { get; }
    public FaceDefinition(string eyesAnchor = "eyes", string browsAnchor = "face", int eyesZIndex = 280, int browsZIndex = 290, string? requiredVisiblePart = null)
    {
        EyesAnchor = DefinitionValidation.Id(eyesAnchor, nameof(eyesAnchor)); BrowsAnchor = DefinitionValidation.Id(browsAnchor, nameof(browsAnchor));
        EyesZIndex = eyesZIndex; BrowsZIndex = browsZIndex; RequiredVisiblePart = requiredVisiblePart is null ? null : DefinitionValidation.Id(requiredVisiblePart, nameof(requiredVisiblePart));
    }
}
