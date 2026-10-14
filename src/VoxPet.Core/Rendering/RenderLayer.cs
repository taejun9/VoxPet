using VoxPet.Core.Character;

namespace VoxPet.Core.Rendering;

/// <summary>렌더러가 그대로 표시하는 최종 논리 좌표. UI 타입이나 파일 내용은 보관하지 않는다.</summary>
public sealed record RenderLayer
{
    public string Id { get; }
    public string Asset { get; }
    public Transform2D Transform { get; }
    public AffineTransform2D Matrix { get; }
    public double Width { get; }
    public double Height { get; }
    public RenderMesh? Mesh { get; }
    private readonly bool affine;
    public double X => Matrix.OffsetX;
    public double Y => Matrix.OffsetY;
    public double ScaleX => affine ? Length(Matrix.M11, Matrix.M12) : Transform.ScaleX;
    public double ScaleY => affine ? Length(Matrix.M21, Matrix.M22) : Transform.ScaleY;
    public double Rotation => affine ? Math.Atan2(Matrix.M12, Matrix.M11) * 180 / Math.PI : Transform.Rotation;
    public double Opacity { get; }
    public int ZIndex { get; }

    public RenderLayer(string id, string asset, Transform2D? transform = null, double opacity = 1, int zIndex = 0, AffineTransform2D? matrix = null,
        double width = 512, double height = 512, RenderMesh? mesh = null)
    {
        Id = DefinitionValidation.Id(id, nameof(id));
        Asset = DefinitionValidation.Asset(asset, ".png", nameof(asset));
        Transform = transform ?? Transform2D.Identity;
        affine = matrix is not null; Matrix = matrix ?? AffineTransform2D.FromTransform(Transform);
        Width = DefinitionValidation.Positive(width, nameof(width)); Height = DefinitionValidation.Positive(height, nameof(height)); Mesh = mesh;
        DefinitionValidation.Finite(ScaleX, nameof(ScaleX)); DefinitionValidation.Finite(ScaleY, nameof(ScaleY));
        Opacity = DefinitionValidation.Unit(opacity, nameof(opacity));
        ZIndex = zIndex;
    }
    private static double Length(double a, double b)
    {
        double magnitude = Math.Max(Math.Abs(a), Math.Abs(b));
        return magnitude == 0 ? 0 : magnitude * Math.Sqrt(Math.Pow(a / magnitude, 2) + Math.Pow(b / magnitude, 2));
    }
}
