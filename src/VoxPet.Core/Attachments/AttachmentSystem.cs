using VoxPet.Core.Actions;
using VoxPet.Core.Character;
using VoxPet.Core.Motion;

namespace VoxPet.Core.Attachments;

public sealed record ResolvedAttachmentLayer(string Id, string Asset, AffineTransform2D Matrix, int ZIndex, AttachmentTransform Placement, IReadOnlyList<string> ReplacesParts, string Anchor);

/// <summary>착용 선택·충돌·이미지/Anchor 상속을 Core에서 결정한다.</summary>
public sealed class AttachmentSystem
{
    public static IReadOnlyList<string> Categories { get; } = Array.AsReadOnly(new[]
    { "hat", "glasses", "faceDecoration", "earrings", "top", "bottom", "onePiece", "shoes", "outerwear", "cape", "headAccessory", "backAccessory", "handAccessory", "effect" });
    private readonly IReadOnlyDictionary<string, AttachmentDefinition> definitions;
    private readonly List<string> selected = [];
    public IReadOnlyList<string> EnabledIds => Array.AsReadOnly(selected.ToArray());
    public AttachmentSystem(IEnumerable<AttachmentDefinition> definitions)
    {
        var copy = DefinitionValidation.Copy(definitions, nameof(definitions));
        if (copy.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != copy.Count || copy.Any(d => !Categories.Contains(d.Category) && d.Category != "legacyOutfit"))
            throw new ArgumentException("부착물 ID/분류를 확인하세요.");
        this.definitions = copy.ToDictionary(d => d.Id, StringComparer.Ordinal);
    }
    public static bool Conflicts(string first, string second) =>
        first == second && first is not ("faceDecoration" or "earrings" or "effect") ||
        first == "onePiece" && second is "top" or "bottom" || second == "onePiece" && first is "top" or "bottom";
    private static bool Conflicts(AttachmentDefinition a, AttachmentDefinition b) => Conflicts(a.Category, b.Category) ||
        a.ExclusiveGroup is not null && a.ExclusiveGroup == b.ExclusiveGroup;
    public void SetEnabled(string id, bool enabled)
    {
        if (!definitions.TryGetValue(id, out var item)) throw new ArgumentException("부착물이 없습니다.", nameof(id));
        selected.Remove(id); if (!enabled) return;
        selected.RemoveAll(other => Conflicts(item, definitions[other])); selected.Add(id);
    }
    /// <summary>저장된 충돌 선택의 복구는 기존 wardrobe처럼 앞 항목을 우선한다.</summary>
    public void Restore(IEnumerable<string> ids)
    {
        var copy = DefinitionValidation.Copy(ids, nameof(ids));
        if (copy.Any(id => !definitions.ContainsKey(id))) throw new ArgumentException("부착물 선택 참조가 없습니다.");
        selected.Clear();
        foreach (string id in copy)
            if (!selected.Contains(id) && selected.All(other => !Conflicts(definitions[id], definitions[other]))) selected.Add(id);
    }
    public IReadOnlyList<string> ForFrame(IReadOnlyDictionary<string, bool> overrides)
    {
        var result = selected.ToList();
        foreach (var pair in overrides)
        {
            if (!definitions.TryGetValue(pair.Key, out var item)) throw new ArgumentException("부착물 참조가 없습니다.");
            result.Remove(pair.Key);
            if (pair.Value) { result.RemoveAll(other => Conflicts(item, definitions[other])); result.Add(pair.Key); }
        }
        return Array.AsReadOnly(result.ToArray());
    }
    public IReadOnlyList<ResolvedAttachmentLayer> Resolve(RigPose rig, ActionState? action = null, IReadOnlySet<string>? visibleParts = null, IEnumerable<string>? ids = null, string? compatibilityTag = null, double time = 0)
    {
        ArgumentNullException.ThrowIfNull(rig); action ??= ActionState.Idle;
        var choices = ids is null ? EnabledIds : DefinitionValidation.Copy(ids, nameof(ids)); var layers = new List<ResolvedAttachmentLayer>();
        foreach (string id in choices)
        {
            if (!definitions.TryGetValue(id, out var item)) throw new ArgumentException("부착물이 없습니다.");
            if (item.CompatibilityTag is not null && action.ActionId is not null && compatibilityTag != item.CompatibilityTag) continue;
            foreach (var layer in item.Layers)
            {
                if (layer.OnlyDuringAction && !action.IsPlaying || visibleParts is not null && layer.RequiredVisibleParts.Any(part => !visibleParts.Contains(part))) continue;
                string asset = layer.Animation?.FrameAt(time) ?? layer.Asset;
                if (layer.Poses.Count != 0)
                {
                    if (layer.Poses.TryGetValue(action.PoseName ?? "idle", out var posed)) asset = posed;
                    else if (layer.HideOnMissingPose) continue;
                }
                if (!rig.Static.TryGetValue(layer.Anchor, out var baseline)) throw new ArgumentException("부착물 Anchor가 없습니다.");
                var bone = AnchorDelta(rig, layer.Anchor, layer.Placement.Follow, layer.Inherit);
                var origin = layer.Placement.Space == AttachmentSpace.Canvas ? bone : bone.Compose(baseline);
                var matrix = origin.Compose(AffineTransform2D.FromTransform(layer.Transform));
                string renderId = "attachment-" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(item.Id)) + "-" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(layer.Id));
                layers.Add(new(renderId, asset, matrix, layer.ZIndex, layer.Placement, layer.ReplacesParts, layer.Anchor));
            }
        }
        return Array.AsReadOnly(layers.OrderBy(l => l.ZIndex).ToArray());
    }
    public IReadOnlyList<ResolvedAttachmentLayer> Compose(RigPose rig, ActionState action, string? compatibilityTag, IReadOnlySet<string>? visibleParts = null, IEnumerable<string>? ids = null, double time = 0)
        => Resolve(rig, action, visibleParts, ids, compatibilityTag, time);
    internal static AffineTransform2D AnchorDelta(RigPose rig, string anchor, double follow, AttachmentInheritance inherit)
    {
        var baseline = rig.Static[anchor]; var animated = rig.Bone(follow)[anchor];
        var delta = animated.Compose(baseline.Inverse()); var bone = Filter(delta, baseline, animated, inherit, 1);
        if (inherit.MouthMotion)
        {
            var extra = baseline.Compose(animated.Inverse()).Compose(rig.Mouth(follow)[anchor]).Compose(baseline.Inverse());
            bone = bone.Compose(extra);
        }
        return bone;
    }
    private static AffineTransform2D Filter(AffineTransform2D delta, AffineTransform2D baseline, AffineTransform2D animated, AttachmentInheritance inherit, double follow)
    {
        if (follow == 0) return AffineTransform2D.Identity;
        double x = baseline.OffsetX, y = baseline.OffsetY;
        double dx = inherit.Position ? (animated.OffsetX - x) * follow : 0, dy = inherit.Position ? (animated.OffsetY - y) * follow : 0;
        AffineTransform2D linear;
        if (inherit.Rotation && inherit.Scale && follow == 1) linear = new(delta.M11, delta.M12, delta.M21, delta.M22);
        else
        {
            double magnitude = Math.Max(Math.Abs(delta.M11), Math.Abs(delta.M12));
            if (magnitude == 0) throw new ArgumentException("부착물 상속 변환이 퇴화했습니다.");
            double sx = magnitude * Math.Sqrt(Math.Pow(delta.M11 / magnitude, 2) + Math.Pow(delta.M12 / magnitude, 2));
            double cos = delta.M11 / sx, sin = delta.M12 / sx;
            double shear = cos * delta.M21 + sin * delta.M22, sy = -sin * delta.M21 + cos * delta.M22;
            double rotation = inherit.Rotation ? Math.Atan2(sin, cos) * 180 / Math.PI * follow : 0;
            double scaleX = inherit.Scale ? 1 + (sx - 1) * follow : 1, scaleY = inherit.Scale ? 1 + (sy - 1) * follow : 1;
            var rotated = AffineTransform2D.FromTransform(new(rotation: rotation));
            linear = rotated.Compose(new(scaleX, 0, inherit.Scale ? shear * follow : 0, scaleY));
        }
        return new AffineTransform2D(offsetX: x + dx, offsetY: y + dy).Compose(linear).Compose(new(offsetX: -x, offsetY: -y));
    }
}
