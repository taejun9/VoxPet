using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Motion;
using VoxPet.Core.Packages;

namespace VoxPet.Core.Editing;

/// <summary>명시적 edit transaction용 draft. UI/JSON/tick와 독립이며 commit 시 다시 불변 정의를 검증한다.</summary>
public sealed class CharacterDraft
{
    public double Width { get; set; }
    public double Height { get; set; }
    public List<CharacterPart> Parts { get; }
    public List<AnchorDefinition> Anchors { get; }
    public MouthDefinition? Mouth { get; set; }
    public SpriteSheetDefinition? SpriteSheet { get; set; }
    public List<ExpressionDefinition> Expressions { get; }
    public List<PoseDefinition> Poses { get; }
    public List<ActionDefinition> Actions { get; }
    public List<AttachmentDefinition> Attachments { get; }
    public MotionProfile Motion { get; set; }
    public FaceDefinition Face { get; set; }
    public CharacterDraft(CharacterDefinition definition)
    {
        Width = definition.CanvasWidth; Height = definition.CanvasHeight;
        Parts = [.. definition.Parts]; Anchors = [.. definition.Anchors]; Expressions = [.. definition.Expressions];
        Poses = [.. definition.Poses]; Actions = [.. definition.Actions]; Attachments = [.. definition.Attachments];
        Mouth = definition.Mouth; SpriteSheet = definition.SpriteSheet; Motion = definition.Motion; Face = definition.Face;
    }
    public CharacterDefinition Build() => new(Parts, Anchors, Width, Height, mouth: Mouth, expressions: Expressions, poses: Poses,
        actions: Actions, attachments: Attachments, motion: Motion, face: Face, spriteSheet: SpriteSheet);
}

/// <summary>지원 모델/사용하지 않는 PNG도 보존하는 편집 snapshot. 실패한 transaction은 원본을 변경하지 않는다.</summary>
public sealed class CharacterDocument
{
    public VoxPetPackage Package { get; }
    public CharacterDocument(VoxPetPackage package) => Package = package ?? throw new ArgumentNullException(nameof(package));
    public CharacterDocument Edit(Action<CharacterDraft> edit, VoxPetManifest? metadata = null, IReadOnlyDictionary<string, byte[]>? addedAssets = null)
    {
        ArgumentNullException.ThrowIfNull(edit);
        var draft = new CharacterDraft(Package.Character); edit(draft); var definition = draft.Build();
        var manifest = metadata ?? Package.Manifest;
        string[] References(IEnumerable<string> oldIds, IReadOnlyList<string> oldPaths, IEnumerable<string> ids, string directory)
        {
            var previous = oldIds.Zip(oldPaths).ToDictionary(p => p.First, p => p.Second, StringComparer.Ordinal);
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase); var result = new List<string>();
            foreach (string id in ids)
            {
                string path = previous.GetValueOrDefault(id) ?? directory + "/" + id + ".json";
                if (!used.Add(path)) { path = directory + "/" + Guid.NewGuid().ToString("N") + ".json"; used.Add(path); }
                result.Add(path);
            }
            return result.ToArray();
        }
        var actions = References(Package.Character.Actions.Select(a => a.Id), Package.Manifest.Actions, definition.Actions.Select(a => a.Id), "actions");
        var attachments = References(Package.Character.Attachments.Select(a => a.Id), Package.Manifest.Attachments, definition.Attachments.Select(a => a.Id), "attachments");
        manifest = new(manifest.Id, manifest.Name, manifest.Version, manifest.Author, manifest.Description, manifest.Character, manifest.Thumbnail, actions, attachments);
        var assets = Package.AssetPaths.ToDictionary(p => p, p => Package.ReadAsset(p), StringComparer.OrdinalIgnoreCase);
        foreach (var pair in addedAssets ?? new Dictionary<string, byte[]>()) assets[pair.Key] = pair.Value.ToArray();
        return new(new VoxPetPackageValidator().Validate(manifest, definition, assets));
    }
    public CharacterDocument Metadata(string id, string name, string version, string author, string description, string? thumbnail, double width, double height)
        => Edit(d => { d.Width = width; d.Height = height; }, new(id, name, version, author, description,
            Package.Manifest.Character, thumbnail, Package.Manifest.Actions, Package.Manifest.Attachments));
    public byte[] Export() => new VoxPetPackageExporter().Export(Package);
}
