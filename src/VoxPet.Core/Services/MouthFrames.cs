namespace VoxPet.Core.Services;

/// <summary>Gate·정규화·attack/release를 거친 0~1 음량을 상세 시트의 8열로 단조 증가 배정한다.</summary>
public static class MouthFrames
{
    public const int Count = 8;
    private static readonly double[] thresholds = [.02, .10, .22, .38, .56, .74, .90];
    public static int Select(double level)
    {
        level = double.IsFinite(level) ? Math.Clamp(level, 0, 1) : 0;
        int index = 0;
        while (index < thresholds.Length && level >= thresholds[index]) index++;
        return index;
    }
}
