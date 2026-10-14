namespace VoxPet.Core.Character;

/// <summary>분리되지 않은 full-canvas 시트도 같은 정의/패키지/Runtime으로 표현한다.</summary>
public sealed record SpriteSheetExpression
{
    public string Id { get; }
    public bool Blink { get; }
    public IReadOnlyList<string> Open { get; }
    public IReadOnlyList<string> Closed { get; }
    public SpriteSheetExpression(string id, IEnumerable<string> open, IEnumerable<string> closed, bool blink = true)
    {
        Id = DefinitionValidation.Id(id, nameof(id)); Blink = blink;
        Open = DefinitionValidation.Copy(open, nameof(open)); Closed = DefinitionValidation.Copy(closed, nameof(closed));
        if (Open.Count is < 1 or > 16 || Open.Count != Closed.Count) throw new ArgumentException("full-sprite 입/눈 프레임 수를 확인하세요.");
        foreach (string asset in Open.Concat(Closed)) DefinitionValidation.Asset(asset, ".png", nameof(open));
    }
}
public sealed record SpriteSheetDefinition
{
    public string Part { get; }
    public string Curve => Selector.Curve;
    public MouthDefinition Selector { get; }
    public IReadOnlyList<SpriteSheetExpression> Expressions { get; }
    public IEnumerable<string> Assets => Expressions.SelectMany(e => e.Open.Concat(e.Closed));
    public SpriteSheetDefinition(string part, IEnumerable<SpriteSheetExpression> expressions, string curve = "smooth")
    {
        Part = DefinitionValidation.Id(part, nameof(part)); Expressions = DefinitionValidation.Copy(expressions, nameof(expressions));
        if (Expressions.Count == 0 || Expressions.Select(e => e.Id).Distinct(StringComparer.Ordinal).Count() != Expressions.Count ||
            Expressions.Any(e => e.Open.Count != Expressions[0].Open.Count)) throw new ArgumentException("full-sprite 표정 목록/입 프레임 수를 확인하세요.");
        Selector = new(Expressions[0].Open, curve: curve);
    }
}
