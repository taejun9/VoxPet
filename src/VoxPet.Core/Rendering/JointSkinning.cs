using VoxPet.Core.Character;

namespace VoxPet.Core.Rendering;

public static class JointSkinning
{
    public static RenderMesh? Compose(AffineTransform2D delta, PartSkinning skin, double width = 512, double height = 512, AffineTransform2D? rootDelta = null)
    {
        var root = rootDelta ?? AffineTransform2D.Identity;
        if (delta == AffineTransform2D.Identity && root == AffineTransform2D.Identity) return null;
        if (skin.FullY > height) throw new ArgumentException("관절 연결 범위가 Canvas 밖입니다.");
        RigPoint Map(RigPoint p)
        {
            double blend = Math.Clamp((p.Y - skin.RootY) / (skin.FullY - skin.RootY), 0, 1); blend = blend * blend * (3 - 2 * blend);
            var start = root.Map(p.X, p.Y); var moved = delta.Map(p.X, p.Y); return new(start.X + (moved.X - start.X) * blend, start.Y + (moved.Y - start.Y) * blend);
        }
        return Mesh(Map, Rows(skin, height, 4), width);
    }
    /// <summary>원본 팔의 폭을 유지하며 굽힌 팔꿈치를 지난다. 두 endpoint는 각 rig 앵커와 정확히 일치한다.</summary>
    public static RenderMesh? ComposeChain(AffineTransform2D endDelta, AffineTransform2D rootDelta, PartSkinning skin, RigPoint shoulder, RigPoint hand, bool left, double width = 512, double height = 512)
    {
        if (skin.FullY > height) throw new ArgumentException("관절 연결 범위가 Canvas 밖입니다.");
        if (endDelta == rootDelta || Math.Abs(hand.Y - shoulder.Y) < .001 || Math.Abs(skin.FullY - hand.Y) > .1 || endDelta.M11 * endDelta.M22 - endDelta.M12 * endDelta.M21 <= 0 || rootDelta.M11 * rootDelta.M22 - rootDelta.M12 * rootDelta.M21 <= 0) return Compose(endDelta, skin, width, height, rootDelta);
        double CenterX(double y) => shoulder.X + (hand.X - shoulder.X) * (y - shoulder.Y) / (hand.Y - shoulder.Y);
        var restStart = new RigPoint(CenterX(skin.RootY), skin.RootY); var restEnd = new RigPoint(CenterX(skin.FullY), skin.FullY);
        var start = rootDelta.Map(restStart.X, restStart.Y); var end = endDelta.Map(restEnd.X, restEnd.Y);
        double rx = restEnd.X - restStart.X, ry = restEnd.Y - restStart.Y, restLength = Math.Sqrt(rx * rx + ry * ry);
        double dx = end.X - start.X, dy = end.Y - start.Y, distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < .01) return Compose(endDelta, skin, width, height, rootDelta);
        // A wide wing cannot wrap around a tiny-radius elbow without folding its
        // inner contour through itself. Limit curvature while preserving endpoints.
        double bend = Math.Min(Math.Sqrt(Math.Max(0, restLength * restLength - distance * distance)) * .5, distance * .18) * (left ? 1 : -1);
        // A wing whose shoulder already points at the wrist folds lengthwise.
        // Adding a second elbow curve would turn its wide inner contour inside out.
        double aimAngle = Math.Atan2(dy, dx) - Math.Atan2(ry, rx);
        double aligned = Math.Atan2(Math.Sin(aimAngle - Math.Atan2(rootDelta.M12, rootDelta.M11)), Math.Cos(aimAngle - Math.Atan2(rootDelta.M12, rootDelta.M11)));
        if (Math.Abs(aligned) < .001) bend = 0;
        var elbow = new RigPoint((start.X + end.X) * .5 - dy / distance * bend, (start.Y + end.Y) * .5 + dx / distance * bend);
        var control = new RigPoint(elbow.X * 2 - (start.X + end.X) * .5, elbow.Y * 2 - (start.Y + end.Y) * .5);
        double restAngle = Math.Atan2(ry, rx), rootAngle = Math.Atan2(rootDelta.M12, rootDelta.M11);
        double forearmAngle = Math.Atan2(end.Y - control.Y, end.X - control.X) - restAngle;
        double handScale = Math.Sqrt(endDelta.M11 * endDelta.M11 + endDelta.M12 * endDelta.M12);
        // The forearm follows its tangent; the fist can rotate independently at the
        // wrist. Forcing the whole forearm to the pistol's angle twists/folds it.
        var forearm = new AffineTransform2D(offsetX: end.X, offsetY: end.Y)
            .Compose(AffineTransform2D.FromTransform(new(scaleX: handScale, scaleY: handScale, rotation: forearmAngle * 180 / Math.PI)))
            .Compose(new(offsetX: -restEnd.X, offsetY: -restEnd.Y));
        static double Angle(double a) { while (a > Math.PI) a -= Math.PI * 2; while (a < -Math.PI) a += Math.PI * 2; return a; }
        RigPoint Map(RigPoint p)
        {
            if (p.Y <= skin.RootY) return rootDelta.Map(p.X, p.Y);
            if (p.Y >= skin.FullY) return forearm.Map(p.X, p.Y);
            double t = (p.Y - skin.RootY) / (skin.FullY - skin.RootY), u = 1 - t;
            double cx = u * u * start.X + 2 * u * t * control.X + t * t * end.X, cy = u * u * start.Y + 2 * u * t * control.Y + t * t * end.Y;
            double tx = 2 * (u * (control.X - start.X) + t * (end.X - control.X)), ty = 2 * (u * (control.Y - start.Y) + t * (end.Y - control.Y));
            double amount = t; amount = amount * amount * (3 - 2 * amount);
            double angle = rootAngle + Angle(Math.Atan2(ty, tx) - restAngle - rootAngle) * amount;
            double scale = Math.Sqrt(rootDelta.M11 * rootDelta.M11 + rootDelta.M12 * rootDelta.M12) * u + Math.Sqrt(endDelta.M11 * endDelta.M11 + endDelta.M12 * endDelta.M12) * t;
            double offset = (p.X - CenterX(p.Y)) * scale;
            return new(cx + Math.Cos(angle) * offset, cy + Math.Sin(angle) * offset);
        }
        return Mesh(Map, Rows(skin, height, 16), width, 8);
    }
    private static double[] Rows(PartSkinning skin, double height, int divisions) => new[] { 0d, height }.Concat(Enumerable.Range(0, divisions + 1).Select(i => skin.RootY + (skin.FullY - skin.RootY) * i / divisions)).Distinct().Order().ToArray();
    private static RenderMesh Mesh(Func<RigPoint, RigPoint> map, double[] rows, double width, int columns = 4)
    {
        var triangles = new List<RenderTriangle>();
        void Triangle(RigPoint a, RigPoint b, RigPoint c)
        {
            var da = map(a); var db = map(b); var dc = map(c);
            double x1 = b.X - a.X, y1 = b.Y - a.Y, x2 = c.X - a.X, y2 = c.Y - a.Y, det = x1 * y2 - y1 * x2;
            double m11 = ((db.X - da.X) * y2 - (dc.X - da.X) * y1) / det, m21 = (x1 * (dc.X - da.X) - x2 * (db.X - da.X)) / det;
            double m12 = ((db.Y - da.Y) * y2 - (dc.Y - da.Y) * y1) / det, m22 = (x1 * (dc.Y - da.Y) - x2 * (db.Y - da.Y)) / det;
            triangles.Add(new(a, b, c, new(m11, m12, m21, m22, da.X - m11 * a.X - m21 * a.Y, da.Y - m12 * a.X - m22 * a.Y)));
        }
        for (int y = 0; y < rows.Length - 1; y++) for (int x = 0; x < columns; x++)
        {
            var a = new RigPoint(width * x / columns, rows[y]); var b = new RigPoint(width * (x + 1) / columns, rows[y]);
            var c = new RigPoint(b.X, rows[y + 1]); var d = new RigPoint(a.X, rows[y + 1]);
            Triangle(a, b, c); Triangle(a, c, d);
        }
        return new(triangles, joinEdges: true);
    }
}
