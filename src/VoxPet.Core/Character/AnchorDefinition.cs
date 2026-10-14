namespace VoxPet.Core.Character;

/// <summary>부모가 있으면 부모 기준 local 좌표, 없으면 캔버스 좌표이다.</summary>
public sealed record AnchorDefinition
{
    public string Id { get; }
    public string? Parent { get; }
    public Transform2D Transform { get; }
    public double X => Transform.X;
    public double Y => Transform.Y;

    public AnchorDefinition(string id, double x = 0, double y = 0, string? parent = null, Transform2D? transform = null)
    {
        Id = DefinitionValidation.Id(id, nameof(id));
        Parent = parent is null ? null : DefinitionValidation.Id(parent, nameof(parent));
        // transform을 제공해도 별도 x/y 입력의 비유한 값이나 모호한 위치를 허용하지 않는다.
        DefinitionValidation.Finite(x, nameof(x));
        DefinitionValidation.Finite(y, nameof(y));
        if (transform is not null && (x != 0 || y != 0)) throw new ArgumentException("위치는 x/y 또는 transform 중 하나로 지정하세요.");
        Transform = transform ?? new(x, y);
    }
}
