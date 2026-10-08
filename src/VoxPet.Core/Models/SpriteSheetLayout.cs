namespace VoxPet.Core.Models;

/// <summary>기존 입3열 또는 상세 입8열 × 눈2행의 정사각형 셀. 디코딩 전에 크기와 메모리 상한을 검사한다.</summary>
public static class SpriteSheetLayout
{
    public static int Validate(int width, int height)
    {
        int cell = height / 2;
        if (height % 2 != 0 || cell is < 128 or > 1024 ||
            (width != cell * 3 && (width != cell * 8 || cell > 512)))
            throw new ArgumentException("PNG는 정사각형 셀 3열×2행(128~1024px) 또는 8열×2행(128~512px)이어야 합니다.");
        return cell;
    }
    public static int Columns(int width, int height) => width / Validate(width, height);
}
