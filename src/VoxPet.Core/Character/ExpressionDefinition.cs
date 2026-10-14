namespace VoxPet.Core.Character;

public sealed record ExpressionDefinition
{
    public string Id { get; }
    public string Name { get; }
    public string EyesOpen { get; }
    public string EyesClosed { get; }
    public string? Brows { get; }
    public ExpressionDefinition(string id, string eyesOpen, string eyesClosed, string? brows = null, string? name = null)
    {
        Id = DefinitionValidation.Id(id, nameof(id)); Name = DefinitionValidation.Text(name ?? id, nameof(name));
        EyesOpen = DefinitionValidation.Asset(eyesOpen, ".png", nameof(eyesOpen)); EyesClosed = DefinitionValidation.Asset(eyesClosed, ".png", nameof(eyesClosed));
        Brows = brows is null ? null : DefinitionValidation.Asset(brows, ".png", nameof(brows));
    }
}
