using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Models;

namespace VoxPet.Core.Legacy;

public static class LegacyCostumeAdapter
{
    internal static string CategoryName(CostumeCategory category) => category switch
    {
        CostumeCategory.FaceDecoration => "faceDecoration",
        CostumeCategory.OnePiece => "onePiece",
        _ => category.ToString().ToLowerInvariant()
    };
    public static AttachmentDefinition Convert(CostumeProfile profile, string asset, int order = 0)
    {
        ArgumentNullException.ThrowIfNull(profile); profile.Validate();
        string id = profile.Id.ToString("N");
        string category = CategoryName(profile.Category);
        string anchor = profile.EffectiveAttachment switch
        {
            MotionAttachment.Head => "head",
            MotionAttachment.Mouth => "mouth",
            MotionAttachment.LeftHand => "leftHand",
            MotionAttachment.RightHand => "rightHand",
            _ => "root"
        };
        var placement = new AttachmentTransform(new Transform2D(profile.X, profile.Y, profile.Scale, profile.Scale),
            follow: profile.Follow, width: profile.BaseWidth, height: profile.BaseHeight,
            deformWithRig: profile.EffectiveAttachment == MotionAttachment.Garment);
        int zIndex = checked((profile.BehindCharacter ? -1000 : 1000) + order);
        return new(id, profile.Name, category, [new AttachmentLayer("image", asset, anchor, zIndex,
            inherit: new(MouthMotion: profile.EffectiveAttachment == MotionAttachment.Mouth), placement: placement)]);
    }
}
