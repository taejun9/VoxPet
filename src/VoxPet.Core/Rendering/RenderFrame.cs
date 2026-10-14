using VoxPet.Core.Character;

namespace VoxPet.Core.Rendering;

/// <summary>한 프레임의 독립 snapshot. 동일 z-index에서는 입력 순서를 보존한다.</summary>
public sealed record RenderFrame
{
    public IReadOnlyList<RenderLayer> Layers { get; }
    public double CanvasWidth { get; }
    public double CanvasHeight { get; }
    public Transform2D ViewportTransform { get; }

    public RenderFrame(IEnumerable<RenderLayer> layers, double canvasWidth = 512, double canvasHeight = 512, Transform2D? viewportTransform = null)
    {
        CanvasWidth = DefinitionValidation.Positive(canvasWidth, nameof(canvasWidth)); CanvasHeight = DefinitionValidation.Positive(canvasHeight, nameof(canvasHeight));
        ViewportTransform = viewportTransform ?? Transform2D.Identity;
        var copy = DefinitionValidation.Copy(layers, nameof(layers));
        if (copy.Select(layer => layer.Id).Distinct(StringComparer.Ordinal).Count() != copy.Count)
            throw new ArgumentException("렌더 레이어ID가 중복됩니다.", nameof(layers));
        Layers = Array.AsReadOnly(copy.OrderBy(layer => layer.ZIndex).ToArray());
    }
}
