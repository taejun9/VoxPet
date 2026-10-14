using CharacterDefinition = VoxPet.Core.Character.CharacterDefinition;
using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Models;
using VoxPet.Core.Motion;

namespace VoxPet.Core.Legacy;

/// <summary>원본의 mutable collection을 보관하지 않는 무손실 v1 변환 경계.</summary>
public static class LegacyImageSetAdapter
{
    public static string PartId(ImagePart part) => part switch
    {
        ImagePart.ArmL => "arm-left",
        ImagePart.ArmR => "arm-right",
        ImagePart.LegL => "leg-left",
        ImagePart.LegR => "leg-right",
        ImagePart.CapeBack => "cape-back",
        ImagePart.FaceDecoration => "face-decoration",
        _ => part.ToString().ToLowerInvariant()
    };
    public static string AnchorId(ImageAnchor anchor) => anchor switch
    {
        ImageAnchor.Canvas => "root",
        ImageAnchor.Eye => "eyes",
        _ => anchor.ToString().ToLowerInvariant()
    };
    public static CharacterDefinition Convert(ImageSet source)
    {
        ArgumentNullException.ThrowIfNull(source); source.Validate();
        var parts = source.Parts.Select(p => new CharacterPart(PartId(p.Part),
            p.Part is ImagePart.ArmL or ImagePart.ArmR ? "arm" : p.Part is ImagePart.LegL or ImagePart.LegR ? "leg" : PartId(p.Part), p.File,
            p.Part == ImagePart.Head ? "head" : "root", (int)p.Part * 10,
            side: p.Part is ImagePart.ArmL or ImagePart.LegL ? "left" : p.Part is ImagePart.ArmR or ImagePart.LegR ? "right" : null));
        AnchorDefinition[] anchors = [new("root"), new("body", parent: "root"), new("head", parent: "root"),
            new("face", parent: "head"), new("eyes", parent: "head"), new("mouth", parent: "head")];
        var mouth = new MouthDefinition(source.Mouths, curve: source.Mouths.Length == 3 ? "legacy3" : "legacy8",
            frameTransforms: source.MouthOffsets?.Select(offset => new Transform2D(offset.X, offset.Y)), zIndex: (int)ImagePart.Mouth * 10);
        var expressions = source.Expressions.Select(e => new ExpressionDefinition(e.Kind.ToString().ToLowerInvariant(), e.EyesOpen, e.EyesClosed, e.Brows));
        var poses = new List<PoseDefinition>(); var actions = new List<ActionDefinition>();
        for (int a = 0; a < source.Actions.Length; a++)
        {
            var original = source.Actions[a]; var timeline = new List<ActionTimelineEvent>(); double time = 0;
            for (int f = 0; f < original.Frames.Length; f++)
            {
                var frame = original.Frames[f]; string id = $"action-{a}-frame-{f}";
                var sprites = frame.Replace.ToDictionary(pair => PartId(pair.Key), pair => pair.Value);
                var transforms = frame.Anchors?.Where(pair => pair.Key != ImageAnchor.Canvas)
                    .ToDictionary(pair => AnchorId(pair.Key), pair => new Transform2D(pair.Value.X, pair.Value.Y));
                poses.Add(new(id, frame.Pose, sprites, transforms)); timeline.Add(new(time, ActionEventKind.Pose, value: id)); time += frame.DurationMs;
            }
            actions.Add(new($"action-{a}", original.Name, time, timeline,
                original.Template is ActionTemplate.Firearm or ActionTemplate.Bow or ActionTemplate.OneHand or ActionTemplate.TwoHand ? 80 : 50,
                compatibilityTag: original.Template.ToString()));
        }
        var attachments = new List<AttachmentDefinition>();
        for (int o = 0; o < source.Outfits.Length; o++)
            attachments.Add(new($"outfit-{o}", source.Outfits[o].Name, "legacyOutfit", Layers(source.Outfits[o].Layers, false), exclusiveGroup: "legacyOutfit"));
        for (int w = 0; w < source.Weapons.Length; w++)
            attachments.Add(new($"weapon-{w}", source.Weapons[w].Name, "handAccessory", Layers(source.Weapons[w].Layers, true), source.Weapons[w].Template.ToString(), "legacyWeapon"));
        return new(parts, anchors, source.CanvasSize, source.CanvasSize, mouth: mouth, expressions: expressions, poses: poses, actions: actions, attachments: attachments,
            motion: MotionProfile.LegacyImageSet, face: new(eyesZIndex: (int)ImagePart.Eyes * 10, browsZIndex: (int)ImagePart.Brows * 10, requiredVisiblePart: "head"));
    }
    private static IEnumerable<AttachmentLayer> Layers(IEnumerable<PoseLayer> layers, bool weapon) => layers.Select(layer =>
    {
        var required = weapon ? new List<string>() : ImageSet.Targets(layer.Part).Where(p => p is ImagePart.Body or ImagePart.ArmL or ImagePart.ArmR or ImagePart.LegL or ImagePart.LegR).Select(PartId).ToList();
        if (!weapon && layer.Anchor != ImageAnchor.Canvas) required.Add("head");
        return new AttachmentLayer(PartId(layer.Part), layer.Poses["idle"], AnchorId(layer.Anchor), (int)layer.Part * 10 + (weapon ? 0 : 1),
            inherit: new(MouthMotion: layer.Anchor == ImageAnchor.Mouth), poses: layer.Poses, requiredVisibleParts: required,
            onlyDuringAction: layer.Part == ImagePart.Effect, hideOnMissingPose: weapon);
    });
}
