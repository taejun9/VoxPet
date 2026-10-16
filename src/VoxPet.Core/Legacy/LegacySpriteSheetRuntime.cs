using VoxPet.Core.Character;
using VoxPet.Core.Models;
using VoxPet.Core.Rendering;
using VoxPet.Core.Services;

namespace VoxPet.Core.Legacy;

public sealed record LegacyCostumeLayer(CostumeProfile Profile, string Asset);
/// <summary>시트/표정/연결 의상 호환 정책. 모든 선택과 transform을 최종 RenderFrame으로 만든다.</summary>
public sealed class LegacySpriteSheetRuntime : IRuntimeFrameProcessor, IRenderGeometryProvider
{
    private VoxPet.Core.Character.CharacterDefinition? samples;
    private IReadOnlyDictionary<string, VoxPet.Core.Attachments.AttachmentLayer> placements = new Dictionary<string, VoxPet.Core.Attachments.AttachmentLayer>();
    private IReadOnlyDictionary<string, string> sampleCategories = new Dictionary<string, string>();
    public VoxPet.Core.Character.CharacterDefinition? Samples
    {
        get => samples;
        set
        {
            samples = value;
            placements = value?.Attachments.SelectMany(a => a.Layers.Select(l => (Id: "attachment-" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(a.Id)) + "-" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(l.Id)), Layer: l))).ToDictionary(p => p.Id, p => p.Layer)
                ?? new Dictionary<string, VoxPet.Core.Attachments.AttachmentLayer>();
            sampleCategories = value?.Attachments.SelectMany(a => a.Layers.Select(l => (Id: "attachment-" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(a.Id)) + "-" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(l.Id)), a.Category))).ToDictionary(p => p.Id, p => p.Category)
                ?? new Dictionary<string, string>();
        }
    }
    public ExpressionProfile Profile { get; private set; } = ExpressionProfile.Default(0);
    public CharacterParameters Parameters { get; set; } = new(0, 0, 0, 0, 1, MouthState.Closed);
    public CharacterRig Rig { get => geometry.Rig; set => geometry.Rig = value; }
    public HandPlayback? Playback { get; set; }
    public HandMotionProfile? HandProfile { get; set; }
    public MotionPose Motion => geometry.Pose;
    public IReadOnlyList<LegacyCostumeLayer> Costumes { get; set; } = [];
    public bool ShowGuides { get; set; }
    public bool PreserveForegroundWing { get; set; }
    public string? WeaponId { get; set; }
    public bool HoldWeapon { get; set; } = true;
    public int MouthFrames { get; private set; } = 3;
    public string Sprite { get; private set; } = LegacySpriteSheetAdapter.Asset(0, 1);
    public double Blend { get; private set; } = 1;
    public bool HasPrevious { get; private set; }
    public double TearLeftY { get; private set; }
    public double TearRightY { get; private set; }
    public double TearLeftOpacity { get; private set; }
    public double TearRightOpacity { get; private set; }
    public double TearLeftX => Profile.TearLeft * 512 - 5;
    public double TearRightX => Profile.TearRight * 512 - 5;
    private double transitionStart;
    private readonly LegacyRenderGeometryProvider geometry = new();
    private static HandGesture? SampleGesture(string? id) => id switch
    {
        "claw" => HandGesture.Claw,
        "heart" => HandGesture.Heart,
        "toy-gun" or "firearm" => HandGesture.ToyGun,
        "toy-sword" or "one-hand" or "two-hand" => HandGesture.ToySword,
        "magic-wand" or "bow" => HandGesture.MagicWand,
        _ => null
    };
    private void UpdatePose(RuntimeRenderContext context)
    {
        var gesture = Samples != null ? SampleGesture(context.Action.ActionId) : null;
        geometry.Pose = gesture is { } g ? CharacterMotion.Evaluate(Rig, Parameters, context.Input.Time,
            new HandPlayback(g, context.Action.Progress), HandMotionProfile.Default(g)) :
            CharacterMotion.Evaluate(Rig, Parameters, context.Input.Time, Playback, HandProfile);
        if (gesture != null)
        {
            // Preserve the nine timelines' distinct left/right poses, within the original
            // full-sheet rig's safe connected deformation range.
            double left = context.Action.Transforms.GetValueOrDefault("leftShoulder")?.Rotation ?? 0;
            double right = context.Action.Transforms.GetValueOrDefault("rightShoulder")?.Rotation ?? 0;
            geometry.Pose = geometry.Pose with { LeftAngle = Rig.Left.Enabled ? CharacterMotion.SafeAngle(left, Rig.Left) : 0, RightAngle = Rig.Right.Enabled ? CharacterMotion.SafeAngle(right, Rig.Right) : 0 };
        }
    }
    public RenderMesh? Compose(string layerId, bool garment, double follow, RuntimeRenderContext context)
    { UpdatePose(context); return Samples == null ? geometry.Compose(layerId, garment, follow, context) : null; }
    public void Apply(ExpressionProfile profile, int mouthFrames, double now)
    {
        profile.Validate(); _ = LegacySpriteSheetAdapter.Convert(mouthFrames);
        Profile = profile; MouthFrames = mouthFrames; transitionStart = now; HasPrevious = true; Blend = 0;
    }
    public RenderFrame Process(RuntimeRenderContext context, RenderFrame frame)
    {
        var now = context.Input.Time;
        UpdatePose(context);
        int mouth = MouthFrames == Services.MouthFrames.Count ? Services.MouthFrames.Select(Parameters.MouthOpen) : (int)Parameters.Mouth;
        Sprite = LegacySpriteSheetAdapter.Asset(mouth, Profile.Blink && Parameters.EyeOpen < .5 ? 0 : 1);
        Blend = HasPrevious ? ExpressionMotion.Blend(now - transitionStart) : 1;
        if (Blend >= 1) HasPrevious = false;
        var left = ExpressionMotion.Tear(now); var right = ExpressionMotion.Tear(now, .45);
        TearLeftY = Profile.TearTop * 512 + left.Offset; TearRightY = Profile.TearTop * 512 + right.Offset;
        TearLeftOpacity = Profile.Tears ? left.Opacity * Blend : 0; TearRightOpacity = Profile.Tears ? right.Opacity * Blend : 0;
        var layers = new List<RenderLayer>();
        bool separated = Samples != null && frame.Layers.Any(l => l.Id == "part-head");
        var mesh = geometry.Compose("sprite", false, 1, context);
        if (HasPrevious) layers.Add(new("legacy-previous", "legacy/previous.png", opacity: 1 - Blend, mesh: mesh));
        if (separated)
            foreach (var part in frame.Layers.Where(l => l.Id.StartsWith("part-", StringComparison.Ordinal)))
            {
                var matrix = part.Matrix;
                if (context.Action.Transforms.Count == 0 && Motion.Gesture != null)
                {
                    var target = part.Id switch { "part-arm-left" => MotionAttachment.LeftHand, "part-arm-right" => MotionAttachment.RightHand, "part-head" => MotionAttachment.Head, _ => MotionAttachment.Body };
                    matrix = LegacyRenderGeometryProvider.Attachment(Rig, Motion, target, 1).Compose(matrix);
                }
                string asset = part.Id == "part-head" ? CompanionParts.Head(mouth, Profile.Blink && Parameters.EyeOpen < .5 ? 0 : 1) : part.Asset;
                layers.Add(new(part.Id, asset, opacity: part.Opacity * Blend, zIndex: part.ZIndex, matrix: matrix, width: part.Width, height: part.Height));
            }
        else layers.Add(new("legacy-sprite", Sprite, opacity: Blend, zIndex: 1, mesh: mesh));
        foreach (var (x, y, opacity, id) in new[] { (TearLeftX, TearLeftY, TearLeftOpacity, "left"), (TearRightX, TearRightY, TearRightOpacity, "right") })
            if (opacity > 0) layers.Add(new("legacy-tear-" + id, "legacy/tear.png", opacity: opacity, zIndex: 2,
                matrix: LegacyRenderGeometryProvider.Attachment(Rig, Motion, MotionAttachment.Head, 1).Compose(new(offsetX: x, offsetY: y)), width: 10, height: 20));
        int order = 0;
        var activeCategories = frame.Layers.Where(l => sampleCategories.ContainsKey(l.Id)).Select(l => sampleCategories[l.Id]).ToHashSet(StringComparer.Ordinal);
        foreach (var item in Costumes)
        {
            var profile = item.Profile; if (!profile.Enabled) continue;
            // A sample try-on temporarily takes precedence without rewriting saved clothes.
            if (activeCategories.Any(category => VoxPet.Core.Attachments.AttachmentSystem.Conflicts(category, LegacyCostumeAdapter.CategoryName(profile.Category)))) continue;
            var placement = new AffineTransform2D(offsetX: profile.X, offsetY: profile.Y);
            var attachment = profile.EffectiveAttachment;
            var deformation = attachment == MotionAttachment.Garment ? geometry.Compose(item.Asset, true, profile.Follow, context) : null;
            layers.Add(new("legacy-costume-" + profile.Id.ToString("N"), item.Asset, zIndex: (profile.BehindCharacter ? -1000 : 1000) + order++,
                matrix: LegacyRenderGeometryProvider.Attachment(Rig, Motion, attachment, profile.Follow).Compose(placement),
                width: profile.BaseWidth * profile.Scale, height: profile.BaseHeight * profile.Scale, mesh: deformation));
        }
        if (SampleGesture(context.Action.ActionId) == null && Motion.Gesture is { } gesture && Motion.Effect > 0 && (Rig.Left.Enabled || Rig.Right.Enabled))
        {
            var limb = Rig.Right.Enabled ? Rig.Right : Rig.Left;
            var transform = gesture == HandGesture.Heart ? new AffineTransform2D(offsetX: 256 + Motion.HandX, offsetY: Rig.HeadCutY + 60 + Motion.HandY) :
                LegacyRenderGeometryProvider.Attachment(Rig, Motion, Rig.Right.Enabled ? MotionAttachment.RightHand : MotionAttachment.LeftHand, 1)
                    .Compose(new(offsetX: limb.PivotX, offsetY: limb.Y + limb.Height - 16));
            // opaque props의 원점은 (100,100), 음수 좌표도 원본 도형 그대로 보존한다.
            layers.Add(new("legacy-prop-" + gesture, "legacy/prop-" + gesture + ".png", opacity: Motion.Effect, zIndex: 2000,
                matrix: transform.Compose(new(offsetX: -100, offsetY: -100)), width: 200, height: 200));
        }
        if (Samples != null)
        {
            foreach (var rendered in frame.Layers.Where(l => !l.Id.StartsWith("part-", StringComparison.Ordinal) && !l.Id.StartsWith("face-", StringComparison.Ordinal)))
            {
                MotionAttachment target = MotionAttachment.RightHand;
                var matrix = AffineTransform2D.Identity; RenderMesh? deformation = null;
                if (separated)
                {
                    if (context.Action.Effects.Any(e => e.Id == rendered.Id) && CompanionWeapon.All.Any(w => w.Asset == rendered.Asset)) continue;
                    matrix = rendered.Matrix;
                    if (context.Action.Transforms.Count == 0 && Motion.Gesture != null && placements.TryGetValue(rendered.Id, out var binding))
                    {
                        var attachment = binding.Anchor switch { "leftShoulder" or "leftHand" => MotionAttachment.LeftHand, "rightShoulder" or "rightHand" => MotionAttachment.RightHand, "head" => MotionAttachment.Head, _ => MotionAttachment.Body };
                        matrix = LegacyRenderGeometryProvider.Attachment(Rig, Motion, attachment, 1).Compose(matrix);
                    }
                    layers.Add(new(rendered.Id, rendered.Asset, opacity: rendered.Opacity, zIndex: rendered.ZIndex, matrix: matrix, width: rendered.Width, height: rendered.Height));
                    continue;
                }
                if (placements.TryGetValue(rendered.Id, out var layer))
                {
                    target = layer.Anchor switch { "head" => MotionAttachment.Head, "mouth" => MotionAttachment.Mouth, "leftHand" => MotionAttachment.LeftHand, "rightHand" => MotionAttachment.RightHand, _ => MotionAttachment.Body };
                    matrix = LegacyRenderGeometryProvider.Attachment(Rig, Motion, target, layer.Placement.Follow).Compose(AffineTransform2D.FromTransform(layer.Transform));
                    if (layer.Placement.DeformWithRig) deformation = geometry.Compose(layer.Id, true, layer.Placement.Follow, context);
                }
                else if (context.Action.ActionId != "heart") matrix = LegacyRenderGeometryProvider.Attachment(Rig, Motion, target, 1);
                layers.Add(new(rendered.Id, rendered.Asset, opacity: rendered.Opacity, zIndex: rendered.ZIndex, matrix: matrix, width: rendered.Width, height: rendered.Height, mesh: deformation));
            }
            if (!separated && PreserveForegroundWing && frame.Layers.Any(l => placements.TryGetValue(l.Id, out var p) && p.Placement.DeformWithRig))
            {
                // A pelican's visible wing belongs in front of its vest, not under a sleeve.
                var limb = Rig.Right;
                var outline = new[] { (0.035, 0.04), (0.13, -0.02), (0.22, -0.04), (0.32, -0.02), (0.44, 0.04), (0.54, 0.13), (0.66, 0.25), (0.78, 0.40), (0.90, 0.56), (0.98, 0.72), (1.0, 0.82), (0.97, 0.90), (0.89, 0.98), (0.78, 1.01), (0.64, 1.0), (0.48, 0.94), (0.32, 0.84), (0.17, 0.69), (0.0, 0.52), (-0.025, 0.34), (-0.01, 0.16) }
                    .Select(p => new RigPoint(limb.X + p.Item1 * limb.Width, limb.Y + p.Item2 * limb.Height)).ToArray();
                var center = new RigPoint(limb.X + limb.Width * .5, limb.Y + limb.Height * .5);
                var transform = LegacyRenderGeometryProvider.Attachment(Rig, Motion, MotionAttachment.RightHand, 1);
                layers.Add(new("legacy-sample-foreground-wing", Sprite, opacity: Blend, zIndex: 120,
                    mesh: new RenderMesh(outline.Select((p, i) => new RenderTriangle(center, p, outline[(i + 1) % outline.Length], transform)))));
            }
            if (separated && CompanionWeapon.Find(WeaponId) is { } weapon &&
                (context.Action.IsPlaying ? CompanionWeapon.Find(context.Action.ActionId) != null : HoldWeapon))
            {
                var rest = context.Rig.Static["rightHand"];
                var weaponMatrix = context.Rig.Animated["rightHand"].Compose(rest.Inverse());
                layers.Add(new("companion-equipped-weapon", weapon.Asset, zIndex: 150, matrix: weaponMatrix));
            }
        }
        if (ShowGuides) layers.Add(new("legacy-guides", "legacy/guides.png", zIndex: 3000));
        return new(layers, viewportTransform: new(y: -Parameters.BodyBounce));
    }
}
