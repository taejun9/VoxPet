using VoxPet.Core.Character;

namespace VoxPet.Core.Rendering;

public sealed record RenderTriangle
{
    public RigPoint A { get; }
    public RigPoint B { get; }
    public RigPoint C { get; }
    public AffineTransform2D Matrix { get; }
    public RenderTriangle(RigPoint a, RigPoint b, RigPoint c, AffineTransform2D matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix); A = a; B = b; C = c; Matrix = matrix;
    }
}
public sealed record RenderMesh
{
    public IReadOnlyList<RenderTriangle> Triangles { get; }
    public RenderMesh(IEnumerable<RenderTriangle> triangles) => Triangles = DefinitionValidation.Copy(triangles, nameof(triangles));
}
