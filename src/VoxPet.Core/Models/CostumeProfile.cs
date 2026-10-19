namespace VoxPet.Core.Models;

public enum CostumeCategory { Hat, Glasses, FaceDecoration, Earrings, Top, Bottom, Outerwear, OnePiece, Cape }
public enum MotionAttachment { Auto, Body, Head, Mouth, LeftHand, RightHand, Garment }

public sealed record CostumeProfile(Guid Id, string Name, CostumeCategory Category, double X, double Y,
    double BaseWidth, double BaseHeight, double Scale = 1, bool Enabled = true, bool BehindCharacter = false,
    MotionAttachment Attachment = MotionAttachment.Auto, double Follow = 1, string? TargetCharacter = null)
{
    public MotionAttachment EffectiveAttachment => Attachment != MotionAttachment.Auto ? Attachment : Category switch
    {
        CostumeCategory.Hat or CostumeCategory.Glasses or CostumeCategory.Earrings => MotionAttachment.Head,
        CostumeCategory.FaceDecoration => MotionAttachment.Mouth,
        CostumeCategory.Top or CostumeCategory.Outerwear or CostumeCategory.OnePiece => MotionAttachment.Garment,
        _ => MotionAttachment.Body
    };
    public bool FitsCharacter(string? characterId) =>
        string.IsNullOrEmpty(TargetCharacter) || TargetCharacter == "all" || TargetCharacter == characterId;

    public static (double X, double Y, double Width, double Height) DefaultPlacement(CostumeCategory category, string? characterId = null, double originalWidth = 512, double originalHeight = 512)
    {
        bool side = characterId == "pelly";
        double width = category switch
        {
            CostumeCategory.Hat or CostumeCategory.Glasses => 200,
            CostumeCategory.FaceDecoration => 140,
            CostumeCategory.Earrings => 300,
            CostumeCategory.Cape => side ? 280 : 320,
            _ => 230
        };
        double height = width * originalHeight / Math.Max(1, originalWidth);
        if (height > 512) { width *= 512 / height; height = 512; }
        double centerX = (512 - width) / 2 + (side ? -18 : 0);
        double y = category switch
        {
            CostumeCategory.Hat => side ? 10 : 20,
            CostumeCategory.Glasses or CostumeCategory.Earrings => side ? 120 : 140,
            CostumeCategory.FaceDecoration => side ? 170 : 210,
            CostumeCategory.Bottom => side ? 355 : 370,
            CostumeCategory.Cape => side ? 170 : 180,
            _ => side ? 260 : 280
        };
        return (centerX, y, width, height);
    }

    public void Validate()
    {
        if (!Enum.IsDefined(Attachment) || !double.IsFinite(Follow) || Follow is < 0 or > 1) throw new ArgumentException("움직임 연결 범위를 확인하세요.");
        if (TargetCharacter != null && TargetCharacter is not ("neulbo" or "pelly" or "all")) throw new ArgumentException("캐릭터 호환 설정을 확인하세요.");
        if (Id == Guid.Empty || string.IsNullOrWhiteSpace(Name) || Name.Length > 60 || !Enum.IsDefined(Category) ||
            !double.IsFinite(X) || !double.IsFinite(Y) || X is < -512 or > 1024 || Y is < -512 or > 1024 ||
            !double.IsFinite(BaseWidth) || !double.IsFinite(BaseHeight) || BaseWidth is < 1 or > 512 || BaseHeight is < 1 or > 512 ||
            !double.IsFinite(Scale) || Scale is < .05 or > 5) throw new ArgumentException("의상 설정을 확인하세요.");
    }
    public static bool Conflicts(CostumeCategory first, CostumeCategory second) =>
        (first == second && first is not (CostumeCategory.FaceDecoration or CostumeCategory.Earrings)) ||
        (first == CostumeCategory.OnePiece && second is CostumeCategory.Top or CostumeCategory.Bottom) ||
        (second == CostumeCategory.OnePiece && first is CostumeCategory.Top or CostumeCategory.Bottom);
}
