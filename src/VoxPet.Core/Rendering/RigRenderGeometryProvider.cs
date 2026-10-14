using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Motion;

namespace VoxPet.Core.Rendering;

/// <summary>native garment의 Canvas 격자. 세 가까운 Anchor의 filtered delta를 가중하며 source placement를 중복 적용하지 않는다.</summary>
public sealed class RigRenderGeometryProvider : IPlacedRenderGeometryProvider
{
    private readonly CharacterDefinition definition;
    private readonly IReadOnlyDictionary<string, AttachmentLayer> bindings;
    private sealed record Control(RigPoint Point, IReadOnlyList<(string Id, double Weight)> Weights);
    private readonly List<Control> controls = [];
    private readonly string[] boneIds;
    private RigPose? lastRig;
    private readonly Dictionary<(double Follow, AttachmentInheritance Inherit), IReadOnlyDictionary<string, AffineTransform2D>> deltas = [];
    public RigRenderGeometryProvider(CharacterDefinition definition)
    {
        this.definition = definition;
        bindings = definition.Attachments.SelectMany(a => a.Layers.Select(l => new KeyValuePair<string, AttachmentLayer>(
            "attachment-" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(a.Id)) + "-" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(l.Id)), l)))
            .ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        var rest = new RigMotionSolver(definition.Rig).Static;
        for (int y = 0; y < 9; y++) for (int x = 0; x < 9; x++)
        {
            var point = new RigPoint(definition.CanvasWidth * (x / 8d), definition.CanvasHeight * (y / 8d));
            var nearest = rest.Select(b => (b.Key, Distance: double.Hypot((point.X - b.Value.OffsetX) / definition.CanvasWidth, (point.Y - b.Value.OffsetY) / definition.CanvasHeight)))
                .OrderBy(b => b.Distance).Take(3).ToArray();
            var weights = nearest[0].Distance == 0 ? new[] { (Id: nearest[0].Key, Weight: 1d) } :
                nearest.Select(b => (Id: b.Key, Weight: Math.Pow(nearest[0].Distance / b.Distance, 2))).ToArray();
            double total = weights.Sum(w => w.Weight);
            controls.Add(new(point, Array.AsReadOnly(weights.Select(w => (w.Id, w.Weight / total)).ToArray())));
        }
        boneIds = controls.SelectMany(c => c.Weights.Select(w => w.Id)).Distinct(StringComparer.Ordinal).ToArray();
    }
    public RenderMesh? Compose(string layerId, bool garment, double follow, RuntimeRenderContext context) => null;
    public RenderMesh? Compose(ResolvedAttachmentLayer resolved, RuntimeRenderContext context)
    {
        if (resolved.Placement.Follow == 0 || !bindings.TryGetValue(resolved.Id, out var layer)) return null;
        var rig = context.Rig;
        var origin = AttachmentSystem.AnchorDelta(rig, layer.Anchor, layer.Placement.Follow, layer.Inherit);
        if (!ReferenceEquals(lastRig, rig)) { deltas.Clear(); lastRig = rig; }
        if (!deltas.TryGetValue((layer.Placement.Follow, layer.Inherit), out var bones))
        {
            bones = boneIds.ToDictionary(id => id, id => AttachmentSystem.AnchorDelta(rig, id, layer.Placement.Follow, layer.Inherit), StringComparer.Ordinal);
            deltas.Add((layer.Placement.Follow, layer.Inherit), bones);
        }
        if (bones.Values.All(delta => delta == origin)) return null;
        var source = new RigPoint[9, 9]; var target = new RigPoint[9, 9];
        for (int y = 0; y < 9; y++) for (int x = 0; x < 9; x++)
        {
            var control = controls[y * 9 + x]; var point = control.Point;
            source[x, y] = origin.Map(point.X, point.Y); double dx = 0, dy = 0;
            foreach (var weight in control.Weights)
            { var mapped = bones[weight.Id].Map(point.X, point.Y); dx += mapped.X * weight.Weight; dy += mapped.Y * weight.Weight; }
            target[x, y] = new(dx, dy);
        }
        var triangles = new List<RenderTriangle>();
        void Triangle(int ax, int ay, int bx, int by, int cx, int cy)
        {
            var a = source[ax, ay]; var b = source[bx, by]; var c = source[cx, cy]; var da = target[ax, ay]; var db = target[bx, by]; var dc = target[cx, cy];
            double x1 = b.X - a.X, y1 = b.Y - a.Y, x2 = c.X - a.X, y2 = c.Y - a.Y, determinant = x1 * y2 - y1 * x2;
            double m11 = ((db.X - da.X) * y2 - (dc.X - da.X) * y1) / determinant;
            double m21 = (x1 * (dc.X - da.X) - x2 * (db.X - da.X)) / determinant;
            double m12 = ((db.Y - da.Y) * y2 - (dc.Y - da.Y) * y1) / determinant;
            double m22 = (x1 * (dc.Y - da.Y) - x2 * (db.Y - da.Y)) / determinant;
            triangles.Add(new(a, b, c, new(m11, m12, m21, m22, da.X - m11 * a.X - m21 * a.Y, da.Y - m12 * a.X - m22 * a.Y)));
        }
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) { Triangle(x, y, x + 1, y, x + 1, y + 1); Triangle(x, y, x + 1, y + 1, x, y + 1); }
        return new(triangles);
    }
}
