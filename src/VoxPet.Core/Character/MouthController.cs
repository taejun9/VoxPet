namespace VoxPet.Core.Character;

public static class MouthController
{
    private static readonly double[] thresholds = [.02, .10, .22, .38, .56, .74, .90];
    public static int Select(MouthDefinition mouth, double voiceLevel)
    {
        ArgumentNullException.ThrowIfNull(mouth); double level = double.IsFinite(voiceLevel) ? Math.Clamp(voiceLevel, 0, 1) : 0;
        if (mouth.Curve == "legacy3") return level < .2 ? 0 : level < .6 ? 1 : 2;
        if (mouth.Curve == "legacy8") { int index = 0; while (index < thresholds.Length && level >= thresholds[index]) index++; return index; }
        double curve = mouth.Curve == "smooth" ? level * level * (3 - 2 * level) : level;
        return (int)Math.Round(curve * (mouth.Frames.Count - 1), MidpointRounding.AwayFromZero);
    }
}
