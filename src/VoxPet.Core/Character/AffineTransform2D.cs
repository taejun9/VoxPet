namespace VoxPet.Core.Character;

/// <summary>부모 scale/rotation 합성에서 shear를 잃지 않는 UI 독립 affine 변환.</summary>
public sealed record AffineTransform2D
{
    public static AffineTransform2D Identity { get; } = new();
    public double M11 { get; }
    public double M12 { get; }
    public double M21 { get; }
    public double M22 { get; }
    public double OffsetX { get; }
    public double OffsetY { get; }
    public AffineTransform2D(double m11 = 1, double m12 = 0, double m21 = 0, double m22 = 1, double offsetX = 0, double offsetY = 0)
    {
        M11 = DefinitionValidation.Finite(m11, nameof(m11)); M12 = DefinitionValidation.Finite(m12, nameof(m12));
        M21 = DefinitionValidation.Finite(m21, nameof(m21)); M22 = DefinitionValidation.Finite(m22, nameof(m22));
        OffsetX = DefinitionValidation.Finite(offsetX, nameof(offsetX)); OffsetY = DefinitionValidation.Finite(offsetY, nameof(offsetY));
    }
    public static AffineTransform2D FromTransform(Transform2D transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        double radians = transform.Rotation % 360 * Math.PI / 180, cos = Math.Cos(radians), sin = Math.Sin(radians);
        return new(cos * transform.ScaleX, sin * transform.ScaleX, -sin * transform.ScaleY, cos * transform.ScaleY, transform.X, transform.Y);
    }
    /// <summary>local을 먼저 적용한 뒤 이 parent 변환을 적용한다.</summary>
    public AffineTransform2D Compose(AffineTransform2D local)
    {
        ArgumentNullException.ThrowIfNull(local);
        return new(M11 * local.M11 + M21 * local.M12, M12 * local.M11 + M22 * local.M12,
            M11 * local.M21 + M21 * local.M22, M12 * local.M21 + M22 * local.M22,
            M11 * local.OffsetX + M21 * local.OffsetY + OffsetX, M12 * local.OffsetX + M22 * local.OffsetY + OffsetY);
    }
    public RigPoint Map(double x, double y)
    {
        DefinitionValidation.Finite(x, nameof(x)); DefinitionValidation.Finite(y, nameof(y));
        return new(M11 * x + M21 * y + OffsetX, M12 * x + M22 * y + OffsetY);
    }
    public AffineTransform2D Inverse()
    {
        // 정규화한 determinant로 큰 유한 배율의 중간 곱 overflow를 피한다.
        double magnitude = Math.Max(Math.Max(Math.Abs(M11), Math.Abs(M12)), Math.Max(Math.Abs(M21), Math.Abs(M22)));
        if (magnitude == 0) throw new InvalidOperationException("역변환할 수 없는 affine입니다.");
        double a = M11 / magnitude, b = M12 / magnitude, c = M21 / magnitude, d = M22 / magnitude;
        double determinant = a * d - b * c;
        if (determinant == 0) throw new InvalidOperationException("역변환할 수 없는 affine입니다.");
        double factor = (1 / magnitude) / determinant;
        double m11 = d * factor, m12 = -b * factor, m21 = -c * factor, m22 = a * factor;
        return new(m11, m12, m21, m22, -(m11 * OffsetX + m21 * OffsetY), -(m12 * OffsetX + m22 * OffsetY));
    }
}

public readonly record struct RigPoint
{
    public double X { get; }
    public double Y { get; }
    public RigPoint(double x, double y)
    {
        X = DefinitionValidation.Finite(x, nameof(x)); Y = DefinitionValidation.Finite(y, nameof(y));
    }
}
