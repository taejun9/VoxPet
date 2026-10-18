using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Motion;

namespace VoxPet.Core.Character;

/// <summary>원본 형식과 독립적인 불변 캐릭터 구성 및 참조 무결성 경계.</summary>
public sealed record CharacterDefinition
{
    public const int CurrentSchemaVersion = 2;
    public const double DefaultCanvasSize = 512;
    public int SchemaVersion { get; }
    public double CanvasWidth { get; }
    public double CanvasHeight { get; }
    public IReadOnlyList<CharacterPart> Parts { get; }
    public IReadOnlyList<AnchorDefinition> Anchors { get; }
    public RigDefinition Rig { get; }
    public MotionProfile Motion { get; }
    public FaceDefinition Face { get; }
    public MouthDefinition? Mouth { get; }
    public SpriteSheetDefinition? SpriteSheet { get; }
    public IReadOnlyList<ExpressionDefinition> Expressions { get; }
    public IReadOnlyList<PoseDefinition> Poses { get; }
    public IReadOnlyList<ActionDefinition> Actions { get; }
    public IReadOnlyList<AttachmentDefinition> Attachments { get; }
    public IEnumerable<string> Assets => Parts.Select(p => p.Asset).Concat(Mouth?.Frames ?? []).Concat(SpriteSheet?.Assets ?? [])
        .Concat(Expressions.SelectMany(e => new[] { e.EyesOpen, e.EyesClosed, e.Brows }.OfType<string>()))
        .Concat(Poses.SelectMany(p => p.Sprites.Values.OfType<string>()))
        .Concat(Attachments.SelectMany(a => a.Layers).SelectMany(l => new[] { l.Asset }.Concat(l.Poses.Values).Concat(l.Animation?.Frames ?? [])))
        .Concat(Actions.SelectMany(a => a.Timeline).Where(e => e.Kind is ActionEventKind.Sprite or ActionEventKind.Effect).Select(e => e.Value!))
        .Distinct(StringComparer.OrdinalIgnoreCase);

    public CharacterDefinition(IEnumerable<CharacterPart> parts, IEnumerable<AnchorDefinition>? anchors = null,
        double canvasWidth = DefaultCanvasSize, double canvasHeight = DefaultCanvasSize, int schemaVersion = CurrentSchemaVersion,
        MouthDefinition? mouth = null, IEnumerable<ExpressionDefinition>? expressions = null, IEnumerable<PoseDefinition>? poses = null,
        IEnumerable<ActionDefinition>? actions = null, IEnumerable<AttachmentDefinition>? attachments = null, MotionProfile? motion = null, FaceDefinition? face = null, SpriteSheetDefinition? spriteSheet = null)
    {
        if (schemaVersion != CurrentSchemaVersion) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
        SchemaVersion = schemaVersion;
        CanvasWidth = DefinitionValidation.Positive(canvasWidth, nameof(canvasWidth));
        CanvasHeight = DefinitionValidation.Positive(canvasHeight, nameof(canvasHeight));
        Parts = DefinitionValidation.Copy(parts, nameof(parts));
        if (Parts.Any(p => p.Skinning?.FullY > canvasHeight)) throw new ArgumentException("파츠 연결 범위가 Canvas 밖입니다.");
        Anchors = DefinitionValidation.Copy(anchors ?? [], nameof(anchors));
        Rig = new(Anchors);
        Motion = motion ?? MotionProfile.Default; Face = face ?? new();
        Mouth = mouth; SpriteSheet = spriteSheet;
        Expressions = DefinitionValidation.Copy(expressions ?? [], nameof(expressions));
        Poses = DefinitionValidation.Copy(poses ?? [], nameof(poses));
        Actions = DefinitionValidation.Copy(actions ?? [], nameof(actions));
        Attachments = DefinitionValidation.Copy(attachments ?? [], nameof(attachments));
        if (Attachments.SelectMany(a => a.Layers).Any(l => l.Placement.Skinning?.FullY > canvasHeight)) throw new ArgumentException("의상 연결 범위가 Canvas 밖입니다.");
        Unique(Expressions.Select(e => e.Id)); Unique(Poses.Select(p => p.Id));
        Unique(Actions.Select(a => a.Id)); Unique(Attachments.Select(a => a.Id));
        Unique(Actions.SelectMany(a => a.Triggers).Select(t => t.Id));
        if (Parts.Count == 0 || Parts.Select(part => part.Id).Distinct(StringComparer.Ordinal).Count() != Parts.Count)
            throw new ArgumentException("중복하지 않는 파츠ID가 최소 하나 필요합니다.", nameof(parts));
        if (Anchors.Select(anchor => anchor.Id).Distinct(StringComparer.Ordinal).Count() != Anchors.Count)
            throw new ArgumentException("Anchor ID가 중복됩니다.", nameof(anchors));

        var byId = Anchors.ToDictionary(anchor => anchor.Id, StringComparer.Ordinal);
        if (Motion.Acting != CompanionActing.None && new[] { "body", "head", "leftFoot", "rightFoot" }.Any(id => !byId.ContainsKey(id)))
            throw new ArgumentException("기본 움직임에는 body/head/leftFoot/rightFoot 앵커가 필요합니다. 일반 리그는 기본 움직임을 없음으로 선택하세요.");
        if (Face.RequiredVisiblePart is not null && !Parts.Any(p => p.Id == Face.RequiredVisiblePart)) throw new ArgumentException("얼굴 표시 파츠 참조가 없습니다.");
        if (spriteSheet != null && (mouth != null || !Parts.Any(p => p.Id == spriteSheet.Part) || spriteSheet.Expressions.Any(e => !Expressions.Any(x => x.Id == e.Id))))
            throw new ArgumentException("full-sprite의 파츠/표정 참조를 확인하세요. 분리 mouth와 함께 사용하지 않습니다.");
        foreach (var part in Parts)
            if (part.Anchor is not null && !byId.ContainsKey(part.Anchor) || part.Skinning?.EndAnchor is { } end && !byId.ContainsKey(end) || part.Skinning?.RootAnchor is { } root && !byId.ContainsKey(root)) throw new ArgumentException("파츠의 Anchor가 없습니다.", nameof(parts));
        foreach (var anchor in Anchors)
            if (anchor.Parent is not null && !byId.ContainsKey(anchor.Parent)) throw new ArgumentException("부모 Anchor가 없습니다.", nameof(anchors));
        if (mouth is not null && !byId.ContainsKey(mouth.Anchor)) throw new ArgumentException("입 Anchor가 없습니다.", nameof(mouth));
        foreach (var layer in Attachments.SelectMany(a => a.Layers))
            if (!byId.ContainsKey(layer.Anchor) || layer.RequiredVisibleParts.Concat(layer.ReplacesParts).Any(id => !Parts.Any(p => p.Id == id)) || layer.Placement.Skinning?.EndAnchor is { } end && !byId.ContainsKey(end) || layer.Placement.Skinning?.RootAnchor is { } root && !byId.ContainsKey(root))
                throw new ArgumentException("부착물의 Anchor/파츠 참조가 없습니다.", nameof(attachments));
        foreach (var pose in Poses)
            if (pose.Sprites.Keys.Any(id => !Parts.Any(p => p.Id == id)) || pose.Transforms.Keys.Any(id => !byId.ContainsKey(id) && !Parts.Any(p => p.Id == id)))
                throw new ArgumentException("포즈 참조가 없습니다.", nameof(poses));
        foreach (var entry in Actions.SelectMany(a => a.Timeline))
        {
            bool valid = entry.Kind switch
            {
                ActionEventKind.Pose => Poses.Any(p => p.Id == entry.Value) || entry.Value == "idle",
                ActionEventKind.Expression => Expressions.Any(e => e.Id == entry.Value),
                ActionEventKind.Attachment => Attachments.Any(a => a.Id == entry.Value),
                ActionEventKind.Transform => byId.ContainsKey(entry.Target!) || Parts.Any(p => p.Id == entry.Target),
                ActionEventKind.Visibility or ActionEventKind.Sprite => Parts.Any(p => p.Id == entry.Target) ||
                    entry.Target == "mouth" && mouth is not null || entry.Target is "eyes" or "brows" && Expressions.Count != 0,
                ActionEventKind.Effect => byId.ContainsKey(entry.Target!) && (entry.EndAnchor == null || byId.ContainsKey(entry.EndAnchor)),
                _ => true
            };
            if (!valid) throw new ArgumentException("액션 이벤트 참조가 없습니다.", nameof(actions));
        }

    }

    private static void Unique(IEnumerable<string> ids)
    {
        var values = ids.ToArray();
        if (values.Distinct(StringComparer.Ordinal).Count() != values.Length) throw new ArgumentException("정의ID가 중복됩니다.");
    }
}
