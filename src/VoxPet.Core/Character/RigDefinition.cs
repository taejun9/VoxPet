namespace VoxPet.Core.Character;

/// <summary>부모 참조를 검증하고 부모가 먼저 오는 순서를 계산한 불변 rig.</summary>
public sealed record RigDefinition
{
    public IReadOnlyList<AnchorDefinition> Anchors { get; }
    internal IReadOnlyList<AnchorDefinition> Ordered { get; }
    public static IReadOnlyList<string> StandardIds { get; } = Array.AsReadOnly(new[]
    { "root", "body", "head", "face", "eyes", "mouth", "leftShoulder", "rightShoulder", "leftHand", "rightHand", "hips", "leftFoot", "rightFoot", "back" });
    public RigDefinition(IEnumerable<AnchorDefinition> anchors)
    {
        Anchors = DefinitionValidation.Copy(anchors, nameof(anchors));
        if (Anchors.Select(a => a.Id).Distinct(StringComparer.Ordinal).Count() != Anchors.Count) throw new ArgumentException("Anchor ID 중복입니다.");
        var byId = Anchors.ToDictionary(a => a.Id, StringComparer.Ordinal);
        if (Anchors.Any(a => a.Parent is not null && !byId.ContainsKey(a.Parent))) throw new ArgumentException("부모 Anchor가 없습니다.");
        var complete = new HashSet<string>(StringComparer.Ordinal); var ordered = new List<AnchorDefinition>();
        foreach (var anchor in Anchors)
        {
            var visiting = new HashSet<string>(StringComparer.Ordinal); var path = new List<string>(); string? current = anchor.Id;
            while (current is not null && !complete.Contains(current))
            {
                if (!visiting.Add(current)) throw new ArgumentException("Anchor 부모가 순환합니다.");
                path.Add(current); current = byId[current].Parent;
            }
            for (int i = path.Count - 1; i >= 0; i--) if (complete.Add(path[i])) ordered.Add(byId[path[i]]);
        }
        Ordered = Array.AsReadOnly(ordered.ToArray());
    }
    public static RigDefinition Standard() => new(StandardIds.Select(id => new AnchorDefinition(id, parent: id switch
    {
        "root" => null,
        "body" or "leftFoot" or "rightFoot" => "root",
        "head" or "leftShoulder" or "rightShoulder" or "hips" or "back" => "body",
        "face" => "head",
        "eyes" or "mouth" => "face",
        "leftHand" => "leftShoulder",
        "rightHand" => "rightShoulder",
        _ => null
    })));
}
