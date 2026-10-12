using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

public readonly record struct MotionPoint(double X, double Y);
/// <summary>연결된 격자 변형. 영역 가장자리에서 영향이0이므로 팔 이동 후 사각 구멍을 남기지 않는다.</summary>
public static class ConnectedMotion
{
    public static MotionPoint Map(MotionPoint point, CharacterRig rig, MotionPose pose, double follow = 1, bool head = true)
    {
        double x = point.X, y = point.Y;
        if (head && y < rig.HeadCutY)
        {
            double influence = Smooth((rig.HeadCutY - y) / 24);
            var target = Rotate(point, 256, rig.HeadCutY, pose.HeadAngle * follow);
            return new(x + (target.X - x) * influence, y + (target.Y - y) * influence);
        }
        foreach (var (limb, angle) in new[] { (rig.Left, pose.LeftAngle), (rig.Right, pose.RightAngle) })
        {
            if (!limb.Enabled || y <= limb.Y || y >= limb.Y + limb.Height || x <= limb.X || x >= limb.X + limb.Width) continue;
            double u = (x - limb.X) / limb.Width, v = (y - limb.Y) / limb.Height;
            double weight = Smooth(Math.Min(u, 1 - u) / .25) * Smooth(Math.Min(v / .25, (1 - v) / .12));
            double scale = 1 + (pose.HandScale - 1) * follow;
            var scaled = new MotionPoint(limb.PivotX + (x - limb.PivotX) * scale, limb.PivotY + (y - limb.PivotY) * scale);
            var target = Rotate(scaled, limb.PivotX, limb.PivotY, angle * follow);
            return new(x + (target.X + pose.HandX * follow - x) * weight, y + (target.Y + pose.HandY * follow - y) * weight);
        }
        return point;
    }
    public static double[] Columns(CharacterRig rig) => new[] { 0d, 256, 512 }.Concat(new[] { rig.Left, rig.Right }.Where(limb => limb.Enabled).SelectMany(limb => new[] { limb.X, limb.X + limb.Width * .25, limb.PivotX, limb.X + limb.Width * .75, limb.X + limb.Width })).Distinct().Order().ToArray();
    public static double[] Rows(CharacterRig rig, bool head) => new[] { head ? rig.HeadCutY - 24 : rig.HeadCutY, rig.HeadCutY, 512d }.Concat(new[] { rig.Left, rig.Right }.Where(limb => limb.Enabled).SelectMany(limb => new[] { limb.Y, limb.Y + limb.Height * .25, limb.Y + limb.Height * .5, limb.Y + limb.Height * .75, limb.Y + limb.Height * .88, limb.Y + limb.Height })).Distinct().Order().ToArray();
    private static double Smooth(double value) { value = Math.Clamp(value, 0, 1); return value * value * (3 - 2 * value); }
    private static MotionPoint Rotate(MotionPoint point, double x, double y, double degrees)
    {
        double radians = degrees * Math.PI / 180, cos = Math.Cos(radians), sin = Math.Sin(radians);
        return new(x + (point.X - x) * cos - (point.Y - y) * sin, y + (point.X - x) * sin + (point.Y - y) * cos);
    }
}
