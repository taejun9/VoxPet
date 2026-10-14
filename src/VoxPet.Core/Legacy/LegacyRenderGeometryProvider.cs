using VoxPet.Core.Character;
using VoxPet.Core.Models;
using VoxPet.Core.Rendering;
using VoxPet.Core.Services;

namespace VoxPet.Core.Legacy;

/// <summary>WPF가 사용하던 연결 격자와 Anchor 변환을 UI 없이 계산한다.</summary>
public sealed class LegacyRenderGeometryProvider : IRenderGeometryProvider
{
    public CharacterRig Rig { get; set; } = CharacterRig.Default(CharacterCatalog.DefaultId);
    public MotionPose Pose { get; set; } = MotionPose.Rest;
    private readonly Dictionary<(bool Head, double Follow), RenderMesh> cache = [];
    private CharacterRig? previousRig;
    private MotionPose? previousPose;
    public RenderMesh? Compose(string layerId, bool garment, double follow, RuntimeRenderContext context)
        => garment ? (Pose.Gesture == null || follow == 0 ? null : Mesh(false, follow)) : (Pose.Gesture == null && Pose.HeadAngle == 0 ? null : Mesh(Pose.HeadAngle != 0, follow));
    public static AffineTransform2D Attachment(CharacterRig rig, MotionPose pose, MotionAttachment attachment, double follow)
    {
        static AffineTransform2D Move(double x, double y) => new(offsetX: x, offsetY: y);
        static AffineTransform2D Around(double x, double y, double rotation, double scale = 1)
            => Move(x, y).Compose(AffineTransform2D.FromTransform(new(scaleX: scale, scaleY: scale, rotation: rotation))).Compose(Move(-x, -y));
        if (attachment is MotionAttachment.Head or MotionAttachment.Mouth)
        {
            var head = Around(256, rig.HeadCutY, pose.HeadAngle * follow);
            return attachment == MotionAttachment.Mouth ? head.Compose(Move(0, pose.MouthY * follow)) : head;
        }
        if (attachment is MotionAttachment.LeftHand or MotionAttachment.RightHand)
        {
            var limb = attachment == MotionAttachment.LeftHand ? rig.Left : rig.Right;
            if (!limb.Enabled) return AffineTransform2D.Identity;
            return Move(pose.HandX * follow, pose.HandY * follow).Compose(Around(limb.PivotX, limb.PivotY,
                (attachment == MotionAttachment.LeftHand ? pose.LeftAngle : pose.RightAngle) * follow, 1 + (pose.HandScale - 1) * follow));
        }
        return AffineTransform2D.Identity;
    }
    public RenderMesh Mesh(bool includeHead, double follow)
    {
        if (previousRig != Rig || previousPose != Pose) { cache.Clear(); previousRig = Rig; previousPose = Pose; }
        if (cache.TryGetValue((includeHead, follow), out var result)) return result;
        var triangles = new List<RenderTriangle>();
        void Triangle(RigPoint a, RigPoint b, RigPoint c, AffineTransform2D? fixedMatrix = null)
        {
            MotionPoint Map(RigPoint p) => ConnectedMotion.Map(new(p.X, p.Y), Rig, Pose, follow, includeHead);
            var da = Map(a); var db = Map(b); var dc = Map(c);
            double x1 = b.X - a.X, y1 = b.Y - a.Y, x2 = c.X - a.X, y2 = c.Y - a.Y, det = x1 * y2 - y1 * x2;
            double m11 = ((db.X - da.X) * y2 - (dc.X - da.X) * y1) / det;
            double m21 = (x1 * (dc.X - da.X) - x2 * (db.X - da.X)) / det;
            double m12 = ((db.Y - da.Y) * y2 - (dc.Y - da.Y) * y1) / det;
            double m22 = (x1 * (dc.Y - da.Y) - x2 * (db.Y - da.Y)) / det;
            triangles.Add(new(a, b, c, fixedMatrix ?? new(m11, m12, m21, m22, da.X - m11 * a.X - m21 * a.Y, da.Y - m12 * a.X - m22 * a.Y)));
        }
        double cut = includeHead ? Rig.HeadCutY - 24 : Rig.HeadCutY;
        var head = includeHead ? Attachment(Rig, Pose, MotionAttachment.Head, follow) : AffineTransform2D.Identity;
        Triangle(new(0, 0), new(512, 0), new(512, cut), head); Triangle(new(0, 0), new(512, cut), new(0, cut), head);
        var columns = ConnectedMotion.Columns(Rig); var rows = ConnectedMotion.Rows(Rig, includeHead);
        for (int r = 0; r < rows.Length - 1; r++) for (int c = 0; c < columns.Length - 1; c++)
        {
            var a = new RigPoint(columns[c], rows[r]); var b = new RigPoint(columns[c + 1], rows[r]);
            var point = new RigPoint(columns[c + 1], rows[r + 1]); var d = new RigPoint(columns[c], rows[r + 1]);
            Triangle(a, b, point); Triangle(a, point, d);
        }
        return cache[(includeHead, follow)] = new(triangles);
    }
}
