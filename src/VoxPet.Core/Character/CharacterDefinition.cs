namespace VoxPet.Core.Character;

/// <summary>Phase 1 구조 정의. 얼굴/동작/부착물 계약은 후속 단계에서 확장한다.</summary>
public sealed record CharacterDefinition
{
    public const int CurrentSchemaVersion = 2;
    public const double DefaultCanvasSize = 512;
    public int SchemaVersion { get; }
    public double CanvasWidth { get; }
    public double CanvasHeight { get; }
    public IReadOnlyList<CharacterPart> Parts { get; }
    public IReadOnlyList<AnchorDefinition> Anchors { get; }

    public CharacterDefinition(IEnumerable<CharacterPart> parts, IEnumerable<AnchorDefinition>? anchors = null,
        double canvasWidth = DefaultCanvasSize, double canvasHeight = DefaultCanvasSize, int schemaVersion = CurrentSchemaVersion)
    {
        if (schemaVersion != CurrentSchemaVersion) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        SchemaVersion = schemaVersion;
        CanvasWidth = DefinitionValidation.Positive(canvasWidth, nameof(canvasWidth));
        CanvasHeight = DefinitionValidation.Positive(canvasHeight, nameof(canvasHeight));
        Parts = DefinitionValidation.Copy(parts, nameof(parts));
        Anchors = DefinitionValidation.Copy(anchors ?? [], nameof(anchors));
        if (Parts.Count == 0 || Parts.Select(part => part.Id).Distinct(StringComparer.Ordinal).Count() != Parts.Count)
            throw new ArgumentException("중복하지 않는 파츠ID가 최소 하나 필요합니다.", nameof(parts));
        if (Anchors.Select(anchor => anchor.Id).Distinct(StringComparer.Ordinal).Count() != Anchors.Count)
            throw new ArgumentException("Anchor ID가 중복됩니다.", nameof(anchors));

        var byId = Anchors.ToDictionary(anchor => anchor.Id, StringComparer.Ordinal);
        foreach (var part in Parts)
            if (part.Anchor is not null && !byId.ContainsKey(part.Anchor)) throw new ArgumentException("파츠의 Anchor가 없습니다.", nameof(parts));
        foreach (var anchor in Anchors)
            if (anchor.Parent is not null && !byId.ContainsKey(anchor.Parent)) throw new ArgumentException("부모 Anchor가 없습니다.", nameof(anchors));

        // 반복 순회로 깊은 사용자 계층에서도 stack overflow 없이 cycle을 검사한다.
        var complete = new HashSet<string>(StringComparer.Ordinal);
        foreach (var anchor in Anchors)
        {
            var path = new HashSet<string>(StringComparer.Ordinal);
            string? current = anchor.Id;
            while (current is not null && !complete.Contains(current))
            {
                if (!path.Add(current)) throw new ArgumentException("Anchor 부모 계층이 순환합니다.", nameof(anchors));
                current = byId[current].Parent;
            }
            complete.UnionWith(path);
        }
    }
}
