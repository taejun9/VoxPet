using System.Collections.ObjectModel;
using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Motion;

namespace VoxPet.Core.Rendering;

public sealed record AssetDimensions
{
    public double Width { get; }
    public double Height { get; }
    public AssetDimensions(double width, double height)
    { Width = DefinitionValidation.Positive(width, nameof(width)); Height = DefinitionValidation.Positive(height, nameof(height)); }
}
public sealed record RuntimeRenderContext(RuntimeInput Input, CharacterPose Motion, ActionState Action, RigPose Rig);
/// <summary>호환 이미지의 연결 격자를 pure 계산으로 제공한다. 파일/JSON/UI 접근을 하지 않는다.</summary>
public interface IRenderGeometryProvider
{
    RenderMesh? Compose(string layerId, bool garment, double follow, RuntimeRenderContext context);
}

public interface IPlacedRenderGeometryProvider : IRenderGeometryProvider
{
    RenderMesh? Compose(ResolvedAttachmentLayer layer, RuntimeRenderContext context);
}

/// <summary>기존 full-sprite 등 호환 조합도 Core에서 최종 frame을 결정한다.</summary>
public interface IRuntimeFrameProcessor
{
    RenderFrame Process(RuntimeRenderContext context, RenderFrame frame);
}

public sealed class RenderComposer
{
    private readonly CharacterDefinition definition;
    private readonly IReadOnlyDictionary<string, AssetDimensions> dimensions;
    private readonly IRenderGeometryProvider? geometry;
    public RenderComposer(CharacterDefinition definition, IReadOnlyDictionary<string, AssetDimensions>? dimensions = null, IRenderGeometryProvider? geometry = null)
    {
        ArgumentNullException.ThrowIfNull(definition); this.definition = definition; this.geometry = geometry;
        var copy = new Dictionary<string, AssetDimensions>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in dimensions ?? new Dictionary<string, AssetDimensions>())
        { DefinitionValidation.Asset(pair.Key, ".png", nameof(dimensions)); ArgumentNullException.ThrowIfNull(pair.Value); copy.Add(pair.Key, pair.Value); }
        this.dimensions = new ReadOnlyDictionary<string, AssetDimensions>(copy);
    }
    public RenderFrame Compose(RuntimeRenderContext context, string expressionId, int mouthFrame, AttachmentSystem attachments, IEnumerable<string> selectedIds, Transform2D viewport)
    {
        var action = context.Action; var rig = context.Rig; var layers = new List<RenderLayer>(); var visible = new HashSet<string>(StringComparer.Ordinal);
        var jointMeshes = new Dictionary<(AffineTransform2D Matrix, PartSkinning Skin, string? Shoulder), RenderMesh?>();
        RenderMesh? Joint(AffineTransform2D matrix, PartSkinning skin, string? shoulderAnchor)
        {
            if (!jointMeshes.TryGetValue((matrix, skin, shoulderAnchor), out var mesh))
            {
                var root = skin.RootAnchor == null ? AffineTransform2D.Identity : Anchor(skin.RootAnchor);
                mesh = skin.EndAnchor is { } hand && shoulderAnchor is { } shoulder && rig.Static.ContainsKey(shoulder) ?
                    JointSkinning.ComposeChain(matrix, root, skin, rig.Static[shoulder].Map(0, 0), rig.Static[hand].Map(0, 0), hand.StartsWith("left", StringComparison.Ordinal), definition.CanvasWidth, definition.CanvasHeight) :
                    JointSkinning.Compose(matrix, skin, definition.CanvasWidth, definition.CanvasHeight, root);
                jointMeshes.Add((matrix, skin, shoulderAnchor), mesh);
            }
            return mesh;
        }
        AffineTransform2D Anchor(string? id, bool mouth = false)
        {
            var world = mouth ? rig.WithMouth : rig.Animated;
            return id is not null && rig.Static.TryGetValue(id, out var baseline) ? world[id].Compose(baseline.Inverse()) : AffineTransform2D.Identity;
        }
        string? Sprite(string target, string? baseline)
        {
            if (action.Visibility.TryGetValue(target, out bool enabled) && !enabled) return null;
            return action.Sprites.TryGetValue(target, out var replacement) ? replacement : baseline;
        }
        void Layer(string id, string asset, int z, AffineTransform2D matrix, double? width = null, double? height = null, RenderMesh? mesh = null)
            => layers.Add(new(id, asset, zIndex: z, matrix: matrix, width: width ?? definition.CanvasWidth, height: height ?? definition.CanvasHeight, mesh: mesh));
        var fullSprite = definition.SpriteSheet?.Expressions.FirstOrDefault(e => e.Id == expressionId) ?? definition.SpriteSheet?.Expressions.FirstOrDefault();
        foreach (var part in definition.Parts)
        {
            if (!action.Visibility.GetValueOrDefault(part.Id, part.Visible)) continue;
            string baseline = part.Asset;
            if (part.Id == definition.SpriteSheet?.Part && fullSprite != null)
                baseline = fullSprite.Blink && (context.Input.Blink ?? context.Motion.EyeOpen < .5) ? fullSprite.Closed[mouthFrame] : fullSprite.Open[mouthFrame];
            string? asset = Sprite(part.Id, baseline); if (asset is null) continue; visible.Add(part.Id);
            var local = AffineTransform2D.FromTransform(part.Transform);
            if (!rig.Static.ContainsKey(part.Id) && action.Transforms.TryGetValue(part.Id, out var transform)) local = local.Compose(AffineTransform2D.FromTransform(transform));
            var matrix = Anchor(part.Anchor);
            Layer("part-" + part.Id, asset, part.ZIndex, part.Skinning == null ? matrix.Compose(local) : local,
                mesh: part.Skinning is { } skin ? Joint(skin.EndAnchor == null ? matrix : Anchor(skin.EndAnchor), skin, part.Anchor) : geometry?.Compose(part.Id, false, 1, context));
        }
        string? required = definition.Face.RequiredVisiblePart ?? definition.Parts.FirstOrDefault(p => p.Type == "head")?.Id;
        bool faceVisible = required is null || visible.Contains(required);
        var expression = definition.Expressions.FirstOrDefault(e => e.Id == expressionId) ?? definition.Expressions.FirstOrDefault(e => e.Id == "neutral") ?? definition.Expressions.FirstOrDefault();
        if (definition.SpriteSheet is null && faceVisible && expression is not null)
        {
            string? eyes = Sprite("eyes", context.Input.Blink ?? context.Motion.EyeOpen < .5 ? expression.EyesClosed : expression.EyesOpen);
            if (eyes is not null) Layer("face-eyes", eyes, definition.Face.EyesZIndex, Anchor(definition.Face.EyesAnchor));
            string? brows = Sprite("brows", expression.Brows);
            if (brows is not null) Layer("face-brows", brows, definition.Face.BrowsZIndex, Anchor(definition.Face.BrowsAnchor));
        }
        if (faceVisible && definition.Mouth is { } mouth)
        {
            string? asset = Sprite("mouth", mouth.Frames[mouthFrame]);
            if (asset is not null) Layer("face-mouth", asset, mouth.ZIndex, Anchor(mouth.Anchor, true));
        }
        foreach (var attachment in attachments.Compose(rig, action, context.Action.ActionId is null ? null : definition.Actions.FirstOrDefault(a => a.Id == context.Action.ActionId)?.CompatibilityTag, visible, selectedIds, context.Input.Time))
        {
            // Resolve visibility first: hidden or incompatible clothing must never
            // remove a body part. Shoes replace the original feet only when drawn.
            layers.RemoveAll(layer => attachment.ReplacesParts.Any(id => layer.Id == "part-" + id));
            var matrix = attachment.Matrix;
            RenderMesh? mesh;
            if (attachment.Placement.Skinning is { } skin)
            { var local = AffineTransform2D.FromTransform(attachment.Placement.Local); mesh = Joint(skin.EndAnchor == null ? matrix.Compose(local.Inverse()) : Anchor(skin.EndAnchor), skin, attachment.Anchor); matrix = local; }
            else mesh = attachment.Placement.DeformWithRig ? geometry is IPlacedRenderGeometryProvider placed ? placed.Compose(attachment, context) : geometry?.Compose(attachment.Id, true, attachment.Placement.Follow, context) : null;
            if (attachment.Placement.DeformWithRig && geometry is null && action.IsPlaying) throw new InvalidOperationException("garment 연결 격자 provider가 필요합니다.");
            Layer(attachment.Id, attachment.Asset, attachment.ZIndex, matrix, attachment.Placement.Width, attachment.Placement.Height, mesh);
        }
        foreach (var effect in action.Effects)
        {
            var matrix = rig.WithMouth[effect.Anchor].Compose(AffineTransform2D.FromTransform(effect.Transform));
            dimensions.TryGetValue(effect.Asset, out var size);
            if (effect.EndAnchor is { } endAnchor)
            {
                var end = rig.WithMouth[endAnchor].Map(effect.EndTransform.X, effect.EndTransform.Y);
                double dx = end.X - matrix.OffsetX, dy = end.Y - matrix.OffsetY, length = Math.Sqrt(dx * dx + dy * dy);
                if (length < .001) continue;
                // Stretch a horizontal rope/arrow asset between the two live anchors.
                // Its thickness is preserved while both endpoints follow their bones.
                matrix = new(dx / (size?.Width ?? definition.CanvasWidth), dy / (size?.Width ?? definition.CanvasWidth), -dy / length, dx / length, matrix.OffsetX, matrix.OffsetY);
            }
            Layer(effect.Id, effect.Asset, effect.ZIndex, matrix, size?.Width, size?.Height);
        }
        return new(layers, definition.CanvasWidth, definition.CanvasHeight, viewport);
    }
}
