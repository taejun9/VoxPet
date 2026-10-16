using VoxPet.Core.Character;

namespace VoxPet.Core.Attachments;

public enum AttachmentSpace { Canvas, Anchor }

/// <summary>이미지 논리 배치와 연결 영향도. Canvas는 기존 full-canvas/wardrobe 좌표를 보존한다.</summary>
public sealed record AttachmentTransform
{
    public Transform2D Local { get; }
    public AttachmentSpace Space { get; }
    public double Follow { get; }
    public double? Width { get; }
    public double? Height { get; }
    public bool DeformWithRig { get; }
    public PartSkinning? Skinning { get; }
    public AttachmentTransform(Transform2D? local = null, AttachmentSpace space = AttachmentSpace.Canvas, double follow = 1,
        double? width = null, double? height = null, bool deformWithRig = false, PartSkinning? skinning = null)
    {
        Local = local ?? Transform2D.Identity;
        if (!Enum.IsDefined(space)) throw new ArgumentOutOfRangeException(nameof(space));
        Space = space; Follow = DefinitionValidation.Unit(follow, nameof(follow));
        if (width.HasValue != height.HasValue) throw new ArgumentException("논리 너비/높이를 함께 지정하세요.");
        Width = width is null ? null : DefinitionValidation.Positive(width.Value, nameof(width));
        Height = height is null ? null : DefinitionValidation.Positive(height.Value, nameof(height));
        DeformWithRig = deformWithRig;
        if (skinning != null && (deformWithRig || space != AttachmentSpace.Canvas)) throw new ArgumentException("파츠 연결은 Canvas 좌표의 단일 연결 방식으로 지정하세요.");
        Skinning = skinning;
    }
}
