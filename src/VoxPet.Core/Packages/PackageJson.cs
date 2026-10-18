using System.Text.Json;
using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Motion;
using VoxPet.Core.Triggers;

namespace VoxPet.Core.Packages;

/// <summary>wire schema와 immutable runtime model의 변환 경계. tick에서 사용하지 않는다.</summary>
public static class PackageJson
{
    private static readonly JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    public static JsonDocument Parse(byte[] bytes)
    {
        var doc = JsonDocument.Parse(bytes.AsMemory(bytes.AsSpan().StartsWith(new byte[] { 239, 187, 191 }) ? 3 : 0), new JsonDocumentOptions { MaxDepth = 32 });
        try { Check(doc.RootElement); return doc; } catch { doc.Dispose(); throw; }
    }
    private static void Check(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in value.EnumerateObject()) { if (!names.Add(property.Name)) throw new VoxPetPackageException("JSON 속성 중복입니다."); Check(property.Value); }
        }
        else if (value.ValueKind == JsonValueKind.Array) foreach (var element in value.EnumerateArray()) Check(element);
    }
    private static JsonElement P(JsonElement element, string name) => element.ValueKind == JsonValueKind.Object ? element.EnumerateObject().FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Value : default;
    private static bool Has(JsonElement e, string name) => P(e, name).ValueKind != JsonValueKind.Undefined;
    private static string? S(JsonElement e, string name, string? fallback = null)
    {
        var value = P(e, name); if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return fallback;
        string result = value.GetString()!; if (result.Length > 2048) throw new VoxPetPackageException("JSON 텍스트가 너무 깁니다."); return result;
    }
    private static double N(JsonElement e, string name, double fallback = 0) => P(e, name).ValueKind == JsonValueKind.Undefined ? fallback : P(e, name).GetDouble();
    private static int I(JsonElement e, string name, int fallback = 0) => P(e, name).ValueKind == JsonValueKind.Undefined ? fallback : P(e, name).GetInt32();
    private static bool B(JsonElement e, string name, bool fallback = false) => P(e, name).ValueKind == JsonValueKind.Undefined ? fallback : P(e, name).GetBoolean();
    private static IEnumerable<JsonElement> A(JsonElement e, string name) => P(e, name).ValueKind == JsonValueKind.Undefined ? [] : P(e, name).EnumerateArray();
    private static string Path(string? path) => PackagePath.Normalize(path ?? throw new VoxPetPackageException("문서/자산 경로가 없습니다."));
    private static Transform2D T(JsonElement e) => new(N(e, "x"), N(e, "y"), N(e, "scaleX", N(e, "scale", 1)), N(e, "scaleY", N(e, "scale", 1)), N(e, "rotation"));
    private static object Transform(Transform2D t) => new { t.X, t.Y, t.ScaleX, t.ScaleY, t.Rotation };
    private static PartSkinning? Skin(JsonElement e) => e.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? null : new(N(e, "rootY"), N(e, "fullY"), S(e, "endAnchor"), S(e, "rootAnchor"));
    private static void Schema(JsonElement e) { if (I(e, "schemaVersion", 2) != 2) throw new VoxPetPackageException("schemaVersion2만 지원합니다."); }
    public static VoxPetManifest ReadManifest(byte[] bytes)
    {
        using var doc = Parse(bytes); var e = doc.RootElement;
        if (S(e, "format") != "voxpet" || I(e, "formatVersion") != 2) throw new VoxPetPackageException("voxpet formatVersion2가 필요합니다.");
        return new(S(e, "id")!, S(e, "name")!, S(e, "version")!, S(e, "author", "")!, S(e, "description", "")!,
            Path(S(e, "character", "character.json")), S(e, "thumbnail") is { } thumbnail ? Path(thumbnail) : null,
            A(e, "actions").Select(a => Path(a.GetString())), A(e, "attachments").Select(a => Path(a.GetString())));
    }
    public static byte[] WriteManifest(VoxPetManifest manifest) => JsonSerializer.SerializeToUtf8Bytes(new
    { manifest.Format, manifest.FormatVersion, manifest.Id, manifest.Name, manifest.Version, manifest.Author, manifest.Description, manifest.Character, manifest.Thumbnail, manifest.Actions, manifest.Attachments }, options);
    public static CharacterDefinition ReadCharacter(byte[] bytes, IEnumerable<ActionDefinition>? actions = null, IEnumerable<AttachmentDefinition>? attachments = null)
    {
        using var doc = Parse(bytes); var e = doc.RootElement; Schema(e); var canvas = P(e, "canvas");
        var parts = A(e, "parts").Select(p => new CharacterPart(S(p, "id")!, S(p, "type", "custom")!, Path(S(p, "file")), S(p, "anchor"), I(p, "zIndex"), T(P(p, "transform")), S(p, "side"), B(p, "visible", true), Skin(P(p, "skinning")))).ToArray();
        var anchors = new List<AnchorDefinition>(); var anchorData = P(e, "anchors");
        if (anchorData.ValueKind == JsonValueKind.Object)
            foreach (var p in anchorData.EnumerateObject()) anchors.Add(new(p.Name, parent: S(p.Value, "parent"), transform: T(p.Value)));
        var mouthData = P(e, "mouth"); MouthDefinition? mouth = null;
        if (mouthData.ValueKind == JsonValueKind.Object)
        {
            if (S(mouthData, "mode", "frames") != "frames") throw new VoxPetPackageException("입 frames mode만 지원합니다.");
            var offsets = Has(mouthData, "frameTransforms") ? A(mouthData, "frameTransforms").Select(T).ToArray() : null;
            mouth = new(A(mouthData, "frames").Select(v => Path(v.GetString())), S(mouthData, "anchor", "mouth")!, S(mouthData, "curve", "smooth")!, offsets, I(mouthData, "zIndex", 300));
        }
        var expressions = A(e, "expressions").Select(v => new ExpressionDefinition(S(v, "id")!, Path(S(v, "eyesOpen")), Path(S(v, "eyesClosed")), S(v, "brows") is { } brows ? Path(brows) : null, S(v, "name"))).ToArray();
        var sheetData = P(e, "spriteSheet"); SpriteSheetDefinition? sheet = null;
        if (sheetData.ValueKind == JsonValueKind.Object)
            sheet = new(S(sheetData, "part")!, A(sheetData, "expressions").Select(v => new SpriteSheetExpression(S(v, "id")!,
                A(v, "open").Select(x => Path(x.GetString())), A(v, "closed").Select(x => Path(x.GetString())), B(v, "blink", true))), S(sheetData, "curve", "smooth")!);
        var poses = A(e, "poses").Select(v => new PoseDefinition(S(v, "id")!, S(v, "name"),
            P(v, "sprites").ValueKind == JsonValueKind.Object ? P(v, "sprites").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ValueKind == JsonValueKind.Null ? null : Path(p.Value.GetString())) : null,
            P(v, "transforms").ValueKind == JsonValueKind.Object ? P(v, "transforms").EnumerateObject().ToDictionary(p => p.Name, p => T(p.Value)) : null)).ToList();
        var actionList = (actions ?? []).ToArray();
        // pose label은 독립 상태 이름으로도 사용할 수 있다. sprite/transform은 별도 이벤트로 지정한다.
        foreach (string id in actionList.SelectMany(a => a.Timeline).Where(t => t.Kind == ActionEventKind.Pose && t.Value != "idle").Select(t => t.Value!).Distinct(StringComparer.Ordinal))
            if (!poses.Any(p => p.Id == id)) poses.Add(new(id));
        var motion = P(e, "motion"); var blink = P(motion, "blink"); var idle = P(motion, "idle"); var voice = P(motion, "voice");
        var profile = new MotionProfile(B(blink, "enabled", true), N(blink, "intervalMin", 2), N(blink, "intervalMax", 6), N(blink, "duration", .14),
            B(idle, "enabled", true), N(idle, "breathing", .4), N(idle, "sway", .2), N(voice, "mouth", 1), N(voice, "bodyBounce", 5),
            N(voice, "headBounce", 2), N(voice, "headRotation", 1.5), N(voice, "mouthTravel"), S(motion, "bodyAnchor", "body")!, S(motion, "headAnchor", "head")!, B(motion, "viewportMotion"));
        var faceData = P(e, "face"); var face = new FaceDefinition(S(faceData, "eyesAnchor", "eyes")!, S(faceData, "browsAnchor", "face")!,
            I(faceData, "eyesZIndex", 280), I(faceData, "browsZIndex", 290), S(faceData, "requiredVisiblePart"));
        return new(parts, anchors, N(canvas, "width", 512), N(canvas, "height", 512), mouth: mouth, expressions: expressions, poses: poses,
            actions: actionList, attachments: attachments, motion: profile, face: face, spriteSheet: sheet);
    }
    public static byte[] WriteCharacter(CharacterDefinition c) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        c.SchemaVersion,
        canvas = new { width = c.CanvasWidth, height = c.CanvasHeight },
        parts = c.Parts.Select(p => new { p.Id, p.Type, file = p.Asset, p.Anchor, p.Side, p.ZIndex, p.Visible, transform = Transform(p.Transform), p.Skinning }),
        anchors = c.Anchors.ToDictionary(a => a.Id, a => new { a.X, a.Y, a.Parent, a.Transform.ScaleX, a.Transform.ScaleY, a.Transform.Rotation }),
        mouth = c.Mouth is null ? null : new { c.Mouth.Anchor, mode = "frames", c.Mouth.Frames, c.Mouth.Curve, c.Mouth.ZIndex, frameTransforms = c.Mouth.FrameTransforms.Select(Transform) },
        spriteSheet = c.SpriteSheet is null ? null : new { c.SpriteSheet.Part, c.SpriteSheet.Curve, expressions = c.SpriteSheet.Expressions.Select(e => new { e.Id, e.Open, e.Closed, e.Blink }) },
        expressions = c.Expressions.Select(v => new { v.Id, v.Name, v.EyesOpen, v.EyesClosed, v.Brows }),
        poses = c.Poses.Select(p => new { p.Id, p.Name, p.Sprites, transforms = p.Transforms.ToDictionary(v => v.Key, v => Transform(v.Value)) }),
        face = new { c.Face.EyesAnchor, c.Face.BrowsAnchor, c.Face.EyesZIndex, c.Face.BrowsZIndex, c.Face.RequiredVisiblePart },
        motion = new
        {
            c.Motion.BodyAnchor,
            c.Motion.HeadAnchor,
            c.Motion.ViewportMotion,
            blink = new { enabled = c.Motion.BlinkEnabled, c.Motion.IntervalMin, c.Motion.IntervalMax, duration = c.Motion.BlinkDuration },
            idle = new { enabled = c.Motion.IdleEnabled, c.Motion.Breathing, c.Motion.Sway },
            voice = new { c.Motion.Mouth, c.Motion.BodyBounce, c.Motion.HeadBounce, c.Motion.HeadRotation, c.Motion.MouthTravel }
        }
    }, options);
    public static ActionDefinition ReadAction(byte[] bytes)
    {
        using var doc = Parse(bytes); var e = doc.RootElement; Schema(e); string id = S(e, "id")!; var playback = P(e, "playback");
        var timeline = A(e, "timeline").Select(ReadEvent).ToArray(); if (timeline.Length > 2048) throw new VoxPetPackageException("timeline은2048이벤트 이내입니다.");
        double derived = timeline.Length == 0 ? 1000 : timeline.Max(t => t.TimeMs + t.DurationMs); if (derived == 0) derived = 1000;
        var mode = S(playback, "mode", "once") switch { "once" => ActionPlaybackMode.Once, "loop" => ActionPlaybackMode.Loop, _ => throw new VoxPetPackageException("재생 mode를 확인하세요.") };
        var triggers = A(e, "triggers").Select((t, i) => ReadTrigger(t, id, i)).ToArray();
        return new(id, S(e, "name", id)!, N(e, "duration", derived), timeline, I(e, "priority", 50), new(mode, B(playback, "interruptible", true), B(playback, "returnToIdle", true)), S(e, "compatibilityTag"), triggers, B(e, "interpolateTransforms"), S(e, "transformInterpolation", "smoothStep") switch
        { "smoothStep" => ActionInterpolation.SmoothStep, "monotoneCubic" => ActionInterpolation.MonotoneCubic, _ => throw new VoxPetPackageException("transformInterpolation을 확인하세요.") });
    }
    private static TriggerDefinition ReadTrigger(JsonElement e, string action, int index)
    {
        var kind = S(e, "type") switch { "hotkey" => TriggerKind.Hotkey, "mouse" => TriggerKind.Mouse, "voiceThreshold" => TriggerKind.VoiceThreshold, "interval" => TriggerKind.Interval, "manual" => TriggerKind.Manual, _ => throw new VoxPetPackageException("trigger type을 확인하세요.") };
        TriggerModifiers modifiers = TriggerModifiers.None;
        foreach (var flag in A(e, "modifiers")) modifiers |= flag.GetString() switch { "ctrl" => TriggerModifiers.Ctrl, "alt" => TriggerModifiers.Alt, "shift" => TriggerModifiers.Shift, _ => throw new VoxPetPackageException("modifier를 확인하세요.") };
        return new(S(e, "id", action + "-trigger-" + index)!, action, kind, S(e, "key", kind == TriggerKind.Manual ? action : null), S(e, "button"), modifiers,
            N(e, "threshold", .5), N(e, "interval", 5), B(e, "startImmediately"), B(e, "enabled", true));
    }
    private static ActionTimelineEvent ReadEvent(JsonElement e)
    {
        string[] kinds = ["pose", "transform", "visibility", "sprite", "effect", "sound", "expression", "attachment"];
        string[] present = kinds.Where(k => Has(e, k)).ToArray(); if (present.Length != 1) throw new VoxPetPackageException("이벤트는 한 payload가 필요합니다.");
        string kind = present[0]; var value = P(e, kind); double time = N(e, "time");
        return kind switch
        {
            "pose" => new(time, ActionEventKind.Pose, value: value.GetString()),
            "transform" => new(time, ActionEventKind.Transform, S(value, "target"), transform: T(value)),
            "visibility" => new(time, ActionEventKind.Visibility, S(value, "target"), enabled: B(value, "visible", true)),
            "sprite" => new(time, ActionEventKind.Sprite, S(value, "target"), Path(S(value, "file"))),
            "effect" => new(time, ActionEventKind.Effect, S(value, "anchor"), Path(S(value, "file")),
                P(value, "transform").ValueKind == JsonValueKind.Object ? T(P(value, "transform")) : null, durationMs: N(value, "duration"), endAnchor: S(value, "endAnchor"), endTransform: Has(value, "endTransform") ? T(P(value, "endTransform")) : null, zIndex: I(value, "zIndex", 2000)),
            "sound" => new(time, ActionEventKind.Sound, value: S(value, "file")),
            "expression" => new(time, ActionEventKind.Expression, value: value.GetString()),
            "attachment" => new(time, ActionEventKind.Attachment, value: S(value, "id"), enabled: B(value, "enabled", true)),
            _ => throw new VoxPetPackageException("이벤트를 확인하세요.")
        };
    }
    private static object Event(ActionTimelineEvent e)
    {
        object payload = e.Kind switch
        {
            ActionEventKind.Pose or ActionEventKind.Expression => e.Value!,
            ActionEventKind.Transform => new { e.Target, e.Transform!.X, e.Transform.Y, e.Transform.ScaleX, e.Transform.ScaleY, e.Transform.Rotation },
            ActionEventKind.Visibility => new { e.Target, visible = e.Enabled },
            ActionEventKind.Sprite => new { e.Target, file = e.Value },
            ActionEventKind.Effect => new { anchor = e.Target, file = e.Value, duration = e.DurationMs, transform = e.Transform is null ? null : Transform(e.Transform), e.EndAnchor, endTransform = Transform(e.EndTransform), e.ZIndex },
            ActionEventKind.Sound => new { file = e.Value },
            ActionEventKind.Attachment => new { id = e.Value, enabled = e.Enabled },
            _ => throw new VoxPetPackageException("이벤트를 확인하세요.")
        };
        return new Dictionary<string, object> { ["time"] = e.TimeMs, [e.Kind.ToString().ToLowerInvariant()] = payload };
    }
    public static byte[] WriteAction(ActionDefinition a) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        schemaVersion = 2,
        a.Id,
        a.Name,
        a.Priority,
        duration = a.DurationMs,
        a.InterpolateTransforms,
        transformInterpolation = a.TransformInterpolation == ActionInterpolation.MonotoneCubic ? "monotoneCubic" : "smoothStep",
        a.CompatibilityTag,
        playback = new { mode = a.Playback.Mode.ToString().ToLowerInvariant(), a.Playback.Interruptible, a.Playback.ReturnToIdle },
        timeline = a.Timeline.Select(Event),
        triggers = a.Triggers.Select(t => new
        {
            t.Id,
            type = t.Kind == TriggerKind.VoiceThreshold ? "voiceThreshold" : t.Kind.ToString().ToLowerInvariant(),
            t.Key,
            t.Button,
            modifiers = new[] { (TriggerModifiers.Ctrl, "ctrl"), (TriggerModifiers.Alt, "alt"), (TriggerModifiers.Shift, "shift") }.Where(m => t.Modifiers.HasFlag(m.Item1)).Select(m => m.Item2),
            t.Threshold,
            interval = t.IntervalSeconds,
            t.StartImmediately,
            t.Enabled
        })
    }, options);
    private static AttachmentAnimation? ReadAnimation(JsonElement e)
    {
        if (e.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return null;
        if (e.ValueKind != JsonValueKind.Object) throw new VoxPetPackageException("부착물 animation은 object여야 합니다.");
        return new(A(e, "frames").Select(v => Path(v.GetString())), N(e, "frameDurationMs", 100));
    }
    public static AttachmentDefinition ReadAttachment(byte[] bytes)
    {
        using var doc = Parse(bytes); var e = doc.RootElement; Schema(e);
        var layers = A(e, "layers").Select((l, index) => new AttachmentLayer(S(l, "id", "layer-" + index)!, Path(S(l, "file")), S(l, "anchor", "root")!, I(l, "zIndex"),
            inherit: new(B(P(l, "inherit"), "position", true), B(P(l, "inherit"), "rotation", true), B(P(l, "inherit"), "scale", true), B(P(l, "inherit"), "mouthMotion")),
            poses: P(l, "poses").ValueKind == JsonValueKind.Object ? P(l, "poses").EnumerateObject().ToDictionary(p => p.Name, p => Path(p.Value.GetString())) : null,
            requiredVisibleParts: A(l, "requiredVisibleParts").Select(v => v.GetString()!), onlyDuringAction: B(l, "onlyDuringAction"), hideOnMissingPose: B(l, "hideOnMissingPose"),
            placement: new(T(P(l, "transform")), S(l, "space", "canvas") switch { "canvas" => AttachmentSpace.Canvas, "anchor" => AttachmentSpace.Anchor, _ => throw new VoxPetPackageException("배치 space를 확인하세요.") },
                N(l, "follow", 1), Has(l, "width") && P(l, "width").ValueKind != JsonValueKind.Null ? N(l, "width") : null,
                Has(l, "height") && P(l, "height").ValueKind != JsonValueKind.Null ? N(l, "height") : null, B(l, "deformWithRig"), Skin(P(l, "skinning"))), animation: ReadAnimation(P(l, "animation")), replacesParts: A(l, "replacesParts").Select(v => v.GetString()!))).ToArray();
        return new(S(e, "id")!, S(e, "name")!, S(e, "category")!, layers, S(e, "compatibilityTag"), S(e, "exclusiveGroup"));
    }
    public static byte[] WriteAttachment(AttachmentDefinition a) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        schemaVersion = 2,
        a.Id,
        a.Name,
        a.Category,
        a.CompatibilityTag,
        a.ExclusiveGroup,
        layers = a.Layers.Select(l => new
        {
            l.Id,
            file = l.Asset,
            l.Anchor,
            l.ZIndex,
            transform = Transform(l.Transform),
            inherit = l.Inherit,
            l.Poses,
            l.RequiredVisibleParts,
            l.ReplacesParts,
            l.OnlyDuringAction,
            l.HideOnMissingPose,
            animation = l.Animation is { } a ? new { a.Frames, a.FrameDurationMs } : null,
            space = l.Placement.Space.ToString().ToLowerInvariant(),
            l.Placement.Follow,
            l.Placement.Width,
            l.Placement.Height,
            l.Placement.Skinning,
            l.Placement.DeformWithRig
        })
    }, options);
}
