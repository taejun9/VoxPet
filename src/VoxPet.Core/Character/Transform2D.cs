namespace VoxPet.Core.Character;

/// <summary>캔버스 논리 좌표 이동, 배율, degree 회전. 음수 배율은 반전을 표현한다.</summary>
public sealed record Transform2D
{
    public static Transform2D Identity { get; } = new();
    public double X { get; }
    public double Y { get; }
    public double ScaleX { get; }
    public double ScaleY { get; }
    public double Rotation { get; }

    public Transform2D(double x = 0, double y = 0, double scaleX = 1, double scaleY = 1, double rotation = 0)
    {
        X = DefinitionValidation.Finite(x, nameof(x));
        Y = DefinitionValidation.Finite(y, nameof(y));
        ScaleX = DefinitionValidation.Finite(scaleX, nameof(scaleX));
        ScaleY = DefinitionValidation.Finite(scaleY, nameof(scaleY));
        Rotation = DefinitionValidation.Finite(rotation, nameof(rotation));
        if (scaleX == 0 || scaleY == 0) throw new ArgumentOutOfRangeException(nameof(scaleX), "배율은0이 될 수 없습니다.");
    }
}
