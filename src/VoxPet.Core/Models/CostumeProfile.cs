namespace VoxPet.Core.Models;

public enum CostumeCategory { Hat, Glasses, FaceDecoration, Earrings, Top, Bottom, Outerwear, OnePiece }

public sealed record CostumeProfile(Guid Id, string Name, CostumeCategory Category, double X, double Y,
    double BaseWidth, double BaseHeight, double Scale = 1, bool Enabled = true)
{
    public void Validate()
    {
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
