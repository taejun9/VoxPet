using System.Collections.ObjectModel;
using VoxPet.Core.Character;

namespace VoxPet.Core.Attachments;

public sealed record AttachmentLayer
{
    public string Id { get; }
    public string Asset { get; }
    public string Anchor { get; }
    public int ZIndex { get; }
    public Transform2D Transform { get; }
    public AttachmentInheritance Inherit { get; }
    public IReadOnlyDictionary<string, string> Poses { get; }
    public IReadOnlyList<string> RequiredVisibleParts { get; }
    public bool OnlyDuringAction { get; }
    public bool HideOnMissingPose { get; }
    public AttachmentLayer(string id, string asset, string anchor = "root", int zIndex = 0, Transform2D? transform = null,
        AttachmentInheritance? inherit = null, IReadOnlyDictionary<string, string>? poses = null,
        IEnumerable<string>? requiredVisibleParts = null, bool onlyDuringAction = false, bool hideOnMissingPose = false)
    {
        Id = DefinitionValidation.Id(id, nameof(id)); Asset = DefinitionValidation.Asset(asset, ".png", nameof(asset));
        Anchor = DefinitionValidation.Id(anchor, nameof(anchor)); ZIndex = zIndex; Transform = transform ?? Transform2D.Identity; Inherit = inherit ?? new();
        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in poses ?? new Dictionary<string, string>())
        {
            DefinitionValidation.Id(pair.Key, nameof(poses)); DefinitionValidation.Asset(pair.Value, ".png", nameof(poses)); copy.Add(pair.Key, pair.Value);
        }
        Poses = new ReadOnlyDictionary<string, string>(copy);
        RequiredVisibleParts = DefinitionValidation.Copy(requiredVisibleParts ?? [], nameof(requiredVisibleParts));
        foreach (string target in RequiredVisibleParts) DefinitionValidation.Id(target, nameof(requiredVisibleParts));
        OnlyDuringAction = onlyDuringAction; HideOnMissingPose = hideOnMissingPose;
    }
}
