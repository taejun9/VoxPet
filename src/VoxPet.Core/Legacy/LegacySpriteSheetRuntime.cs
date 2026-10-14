using VoxPet.Core.Character;
using VoxPet.Core.Models;
using VoxPet.Core.Rendering;
using VoxPet.Core.Services;

namespace VoxPet.Core.Legacy;

public sealed record LegacyCostumeLayer(CostumeProfile Profile, string Asset);
/// <summary>시트/표정/연결 의상 호환 정책. 모든 선택과 transform을 최종 RenderFrame으로 만든다.</summary>
public sealed class LegacySpriteSheetRuntime : IRuntimeFrameProcessor
{
    public ExpressionProfile Profile { get; private set; } = ExpressionProfile.Default(0);
    public CharacterParameters Parameters { get; set; } = new(0, 0, 0, 0, 1, MouthState.Closed);
    public CharacterRig Rig { get => geometry.Rig; set => geometry.Rig = value; }
    public HandPlayback? Playback { get; set; }
    public HandMotionProfile? HandProfile { get; set; }
    public MotionPose Motion => geometry.Pose;
    public IReadOnlyList<LegacyCostumeLayer> Costumes { get; set; } = [];
    public bool ShowGuides { get; set; }
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
    public void Apply(ExpressionProfile profile, int mouthFrames, double now)
    {
        profile.Validate(); _ = LegacySpriteSheetAdapter.Convert(mouthFrames);
        Profile = profile; MouthFrames = mouthFrames; transitionStart = now; HasPrevious = true; Blend = 0;
    }
    public RenderFrame Process(RuntimeRenderContext context, RenderFrame frame)
    {
        var now = context.Input.Time;
        geometry.Pose = CharacterMotion.Evaluate(Rig, Parameters, now, Playback, HandProfile);
        int mouth = MouthFrames == Services.MouthFrames.Count ? Services.MouthFrames.Select(Parameters.MouthOpen) : (int)Parameters.Mouth;
        Sprite = LegacySpriteSheetAdapter.Asset(mouth, Profile.Blink && Parameters.EyeOpen < .5 ? 0 : 1);
        Blend = HasPrevious ? ExpressionMotion.Blend(now - transitionStart) : 1;
        if (Blend >= 1) HasPrevious = false;
        var left = ExpressionMotion.Tear(now); var right = ExpressionMotion.Tear(now, .45);
        TearLeftY = Profile.TearTop * 512 + left.Offset; TearRightY = Profile.TearTop * 512 + right.Offset;
        TearLeftOpacity = Profile.Tears ? left.Opacity * Blend : 0; TearRightOpacity = Profile.Tears ? right.Opacity * Blend : 0;
        var layers = new List<RenderLayer>();
        var mesh = geometry.Compose("sprite", false, 1, context);
        if (HasPrevious) layers.Add(new("legacy-previous", "legacy/previous.png", opacity: 1 - Blend, mesh: mesh));
        layers.Add(new("legacy-sprite", Sprite, opacity: Blend, zIndex: 1, mesh: mesh));
        foreach (var (x, y, opacity, id) in new[] { (TearLeftX, TearLeftY, TearLeftOpacity, "left"), (TearRightX, TearRightY, TearRightOpacity, "right") })
            if (opacity > 0) layers.Add(new("legacy-tear-" + id, "legacy/tear.png", opacity: opacity, zIndex: 2,
                matrix: LegacyRenderGeometryProvider.Attachment(Rig, Motion, MotionAttachment.Head, 1).Compose(new(offsetX: x, offsetY: y)), width: 10, height: 20));
        int order = 0;
        foreach (var item in Costumes)
        {
            var profile = item.Profile; if (!profile.Enabled) continue;
            var placement = new AffineTransform2D(offsetX: profile.X, offsetY: profile.Y);
            var attachment = profile.EffectiveAttachment;
            var deformation = attachment == MotionAttachment.Garment ? geometry.Compose(item.Asset, true, profile.Follow, context) : null;
            layers.Add(new("legacy-costume-" + profile.Id.ToString("N"), item.Asset, zIndex: (profile.BehindCharacter ? -1000 : 1000) + order++,
                matrix: LegacyRenderGeometryProvider.Attachment(Rig, Motion, attachment, profile.Follow).Compose(placement),
                width: profile.BaseWidth * profile.Scale, height: profile.BaseHeight * profile.Scale, mesh: deformation));
        }
        if (Motion.Gesture is { } gesture && Motion.Effect > 0 && (Rig.Left.Enabled || Rig.Right.Enabled))
        {
            var limb = Rig.Right.Enabled ? Rig.Right : Rig.Left;
            var transform = gesture == HandGesture.Heart ? new AffineTransform2D(offsetX: 256 + Motion.HandX, offsetY: Rig.HeadCutY + 60 + Motion.HandY) :
                LegacyRenderGeometryProvider.Attachment(Rig, Motion, Rig.Right.Enabled ? MotionAttachment.RightHand : MotionAttachment.LeftHand, 1)
                    .Compose(new(offsetX: limb.PivotX, offsetY: limb.Y + limb.Height - 16));
            // opaque props의 원점은 (100,100), 음수 좌표도 원본 도형 그대로 보존한다.
            layers.Add(new("legacy-prop-" + gesture, "legacy/prop-" + gesture + ".png", opacity: Motion.Effect, zIndex: 2000,
                matrix: transform.Compose(new(offsetX: -100, offsetY: -100)), width: 200, height: 200));
        }
        if (ShowGuides) layers.Add(new("legacy-guides", "legacy/guides.png", zIndex: 3000));
        return new(layers, viewportTransform: new(y: -Parameters.BodyBounce));
    }
}
