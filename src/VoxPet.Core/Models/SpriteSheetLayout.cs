namespace VoxPet.Core.Models;

/// <summary>Three mouth columns and two eye rows; square cells bounded before decoding.</summary>
public static class SpriteSheetLayout
{
    public static int Validate(int width, int height)
    {
        if (width % 3 != 0 || height % 2 != 0 || width / 3 != height / 2 || width / 3 is < 128 or > 1024)
            throw new ArgumentException("PNG는 정사각형 셀 3열×2행이어야 합니다 (셀 128~1024px).");
        return width / 3;
    }
}
