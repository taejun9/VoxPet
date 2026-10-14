using VoxPet.Core.Character;

namespace VoxPet.Core.Rendering;

/// <summary>한 프레임의 독립 snapshot. 동일 z-index에서는 입력 순서를 보존한다.</summary>
public sealed record RenderFrame
{
    public IReadOnlyList<RenderLayer> Layers { get; }

    public RenderFrame(IEnumerable<RenderLayer> layers)
    {
        var copy = DefinitionValidation.Copy(layers, nameof(layers));
        if (copy.Select(layer => layer.Id).Distinct(StringComparer.Ordinal).Count() != copy.Count)
            throw new ArgumentException("렌더 레이어ID가 중복됩니다.", nameof(layers));
        Layers = Array.AsReadOnly(copy.OrderBy(layer => layer.ZIndex).ToArray());
    }
}
