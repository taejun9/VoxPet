namespace VoxPet.Core.Models;

/// <summary>512기준 수동 영역. PNG와 의상 원본 픽셀을 변경하지 않는다.</summary>
public sealed record LimbRegion(double X, double Y, double Width, double Height, bool Enabled = true)
{
    public double PivotX => X + Width / 2;
    public double PivotY => Y;
    public void Validate()
    {
        if (new[] { X, Y, Width, Height }.Any(value => !double.IsFinite(value)) || X < 0 || Y < 0 || Width < 1 || Height < 1 || X + Width > 512 || Y + Height > 512)
            throw new ArgumentException("손 영역은512안에 있어야 합니다.");
    }
}
public sealed record CharacterRig(double HeadCutY, LimbRegion Left, LimbRegion Right, bool HeadMotion = false, double MouthTravel = 10)
{
    public static CharacterRig Default(string id) => id switch
    {
        "neulbo-man" or "neulbo-girl" => new(214, new(170, 240, 66, 136), new(336, 240, 66, 136)),
        "drawing-neulbo" => new(310, new(134, 325, 58, 130), new(330, 325, 58, 130)),
        "pelly" => new(242, new(100, 264, 50, 130, false), new(312, 264, 148, 150)),
        _ => new(285, new(62, 290, 96, 148), new(354, 290, 96, 148))
    };
    public void Validate()
    {
        if (Left == null || Right == null || !double.IsFinite(HeadCutY) || HeadCutY is < 64 or > 400 || !double.IsFinite(MouthTravel) || MouthTravel is < 0 or > 48)
            throw new ArgumentException("머리/입 연결 범위를 확인하세요.");
        Left.Validate(); Right.Validate();
        if (Left.Enabled && Left.Y < HeadCutY || Right.Enabled && Right.Y < HeadCutY ||
            Left.Enabled && Right.Enabled && Left.X < Right.X + Right.Width && Right.X < Left.X + Left.Width && Left.Y < Right.Y + Right.Height && Right.Y < Left.Y + Left.Height)
            throw new ArgumentException("손 영역은 머리 아래에 서로 겹치지 않게 배치하세요.");
    }
}
