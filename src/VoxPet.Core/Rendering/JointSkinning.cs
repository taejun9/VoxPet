using VoxPet.Core.Character;

namespace VoxPet.Core.Rendering;

public static class JointSkinning
{
    public static RenderMesh? Compose(AffineTransform2D delta, PartSkinning skin, double width = 512, double height = 512)
    {
        if (delta == AffineTransform2D.Identity) return null;
        if (skin.FullY > height) throw new ArgumentException("관절 연결 범위가 Canvas 밖입니다.");
        var rows = new[] { 0, skin.RootY, skin.RootY + (skin.FullY - skin.RootY) * .25, (skin.RootY + skin.FullY) / 2, skin.RootY + (skin.FullY - skin.RootY) * .75, skin.FullY, height }.Distinct().Order().ToArray();
        var triangles = new List<RenderTriangle>();
        RigPoint Map(RigPoint p)
        {
            double blend = Math.Clamp((p.Y - skin.RootY) / (skin.FullY - skin.RootY), 0, 1); blend = blend * blend * (3 - 2 * blend);
            var moved = delta.Map(p.X, p.Y); return new(p.X + (moved.X - p.X) * blend, p.Y + (moved.Y - p.Y) * blend);
        }
        void Triangle(RigPoint a, RigPoint b, RigPoint c)
        {
            var da = Map(a); var db = Map(b); var dc = Map(c);
            double x1 = b.X - a.X, y1 = b.Y - a.Y, x2 = c.X - a.X, y2 = c.Y - a.Y, det = x1 * y2 - y1 * x2;
            double m11 = ((db.X - da.X) * y2 - (dc.X - da.X) * y1) / det, m21 = (x1 * (dc.X - da.X) - x2 * (db.X - da.X)) / det;
            double m12 = ((db.Y - da.Y) * y2 - (dc.Y - da.Y) * y1) / det, m22 = (x1 * (dc.Y - da.Y) - x2 * (db.Y - da.Y)) / det;
            triangles.Add(new(a, b, c, new(m11, m12, m21, m22, da.X - m11 * a.X - m21 * a.Y, da.Y - m12 * a.X - m22 * a.Y)));
        }
        for (int y = 0; y < rows.Length - 1; y++) for (int x = 0; x < 4; x++)
        {
            var a = new RigPoint(width * x / 4, rows[y]); var b = new RigPoint(width * (x + 1) / 4, rows[y]);
            var c = new RigPoint(b.X, rows[y + 1]); var d = new RigPoint(a.X, rows[y + 1]);
            Triangle(a, b, c); Triangle(a, c, d);
        }
        return new(triangles);
    }
}
