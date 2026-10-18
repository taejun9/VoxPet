using VoxPet.Core.Character;

namespace VoxPet.Core.Actions;

/// <summary>Shape-preserving Hermite tracks: continuous velocity without overshooting a grip or recoil key.</summary>
internal static class ActionTransformCurve
{
    public static Transform2D Interpolate(double startTime, Transform2D start, double endTime, Transform2D end, double time,
        double? previousTime, Transform2D? previous, double? followingTime, Transform2D? following, ActionInterpolation curve)
    {
        double h = endTime - startTime, t = Math.Clamp((time - startTime) / h, 0, 1);
        double Blend(double a, double b, double? p, double? n)
        {
            double eased = t * t * (3 - 2 * t), fallback = a * (1 - eased) + b * eased;
            if (curve != ActionInterpolation.MonotoneCubic || a == b) return fallback;
            double d = (b - a) / h;
            double m0 = p.HasValue && previousTime.HasValue ? Tangent((a - p.Value) / (startTime - previousTime.Value), d, startTime - previousTime.Value, h) : 0;
            double m1 = n.HasValue && followingTime.HasValue ? Tangent(d, (n.Value - b) / (followingTime.Value - endTime), h, followingTime.Value - endTime) : 0;
            double value = a * (1 - eased) + b * eased + (t * t * t - 2 * t * t + t) * h * m0 + (t * t * t - t * t) * h * m1;
            return double.IsFinite(value) ? Math.Clamp(value, Math.Min(a, b), Math.Max(a, b)) : fallback;
        }
        double Scale(double a, double b, double? p, double? n) => Math.Sign(a) != Math.Sign(b) ? a
            : Blend(a, b, p.HasValue && Math.Sign(p.Value) == Math.Sign(a) ? p : null, n.HasValue && Math.Sign(n.Value) == Math.Sign(b) ? n : null);
        return new(Blend(start.X, end.X, previous?.X, following?.X), Blend(start.Y, end.Y, previous?.Y, following?.Y),
            Scale(start.ScaleX, end.ScaleX, previous?.ScaleX, following?.ScaleX), Scale(start.ScaleY, end.ScaleY, previous?.ScaleY, following?.ScaleY),
            Blend(start.Rotation, end.Rotation, previous?.Rotation, following?.Rotation));
    }
    private static double Tangent(double before, double after, double h0, double h1)
    {
        if (!double.IsFinite(before) || !double.IsFinite(after) || before == 0 || after == 0 || Math.Sign(before) != Math.Sign(after)) return 0;
        // Normalize intervals before the weighted harmonic mean to avoid time overflow.
        double ratio = h0 / (h0 + h1), w0 = (2 - ratio) / 3, w1 = (1 + ratio) / 3;
        return 1 / (w0 / before + w1 / after);
    }
}
