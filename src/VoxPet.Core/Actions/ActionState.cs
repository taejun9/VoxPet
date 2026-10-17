using System.Collections.ObjectModel;
using VoxPet.Core.Character;

namespace VoxPet.Core.Actions;

public sealed record ActionEffect
{
    public string Id { get; }
    public string Asset { get; }
    public string Anchor { get; }
    public Transform2D Transform { get; }
    public int ZIndex { get; }
    public string? EndAnchor { get; }
    public Transform2D EndTransform { get; }
    public double Progress { get; }
    public ActionEffect(string id, string asset, string anchor, Transform2D transform, double progress, string? endAnchor = null, Transform2D? endTransform = null, int zIndex = 2000)
    {
        Id = DefinitionValidation.Id(id, nameof(id)); Asset = DefinitionValidation.Asset(asset, ".png", nameof(asset));
        Anchor = DefinitionValidation.Id(anchor, nameof(anchor)); ArgumentNullException.ThrowIfNull(transform); Transform = transform;
        ZIndex = zIndex;
        EndAnchor = endAnchor == null ? null : DefinitionValidation.Id(endAnchor, nameof(endAnchor)); EndTransform = endTransform ?? Transform2D.Identity;
        Progress = DefinitionValidation.Unit(progress, nameof(progress));
    }
}

/// <summary>한 시점의 불변 재생 snapshot. effects는 아직 살아 있는 효과만 포함한다.</summary>
public sealed record ActionState
{
    public static ActionState Idle { get; } = new(null, false, 0, 0, null, null, [], [], [], [], null, []);
    public string? ActionId { get; }
    public bool IsPlaying { get; }
    public int Priority { get; }
    public double Progress { get; }
    public string? PoseId { get; }
    public string? PoseName { get; }
    public IReadOnlyDictionary<string, Transform2D> Transforms { get; }
    public IReadOnlyDictionary<string, string?> Sprites { get; }
    public IReadOnlyDictionary<string, bool> Visibility { get; }
    public IReadOnlyList<ActionEffect> Effects { get; }
    public string? ExpressionId { get; }
    public IReadOnlyDictionary<string, bool> Attachments { get; }
    internal ActionState(string? actionId, bool playing, int priority, double progress, string? poseId, string? poseName,
        Dictionary<string, Transform2D> transforms, Dictionary<string, string?> sprites, Dictionary<string, bool> visibility,
        IEnumerable<ActionEffect> effects, string? expressionId, Dictionary<string, bool> attachments)
    {
        ActionId = actionId; IsPlaying = playing; Priority = priority; Progress = progress; PoseId = poseId; PoseName = poseName;
        Transforms = new ReadOnlyDictionary<string, Transform2D>(new Dictionary<string, Transform2D>(transforms, StringComparer.Ordinal));
        Sprites = new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?>(sprites, StringComparer.Ordinal));
        Visibility = new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(visibility, StringComparer.Ordinal));
        Effects = Array.AsReadOnly(effects.ToArray()); ExpressionId = expressionId;
        Attachments = new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>(attachments, StringComparer.Ordinal));
    }
}
