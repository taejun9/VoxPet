namespace VoxPet.Core.Models;

/// <summary>입 3열 × 눈 2행의 정사각형 셀 규격. 이미지 디코딩 전 크기를 제한한다.</summary>
public static class SpriteSheetLayout
{
    /// <summary>
    /// 전체 크기가 3×cell, 2×cell인지 검사하고 셀 한 변의 픽셀 수를 반환한다.
    /// 128~1024px 제한으로 지나치게 작거나 큰 이미지의 로딩을 방지한다.
    /// </summary>
    public static int Validate(int width, int height)
    {
        if (width % 3 != 0 || height % 2 != 0 || width / 3 != height / 2 || width / 3 is < 128 or > 1024)
            throw new ArgumentException("PNG는 정사각형 셀 3열×2행이어야 합니다 (셀 128~1024px).");
        return width / 3;
    }
}
