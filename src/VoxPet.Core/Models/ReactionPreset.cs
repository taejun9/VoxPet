namespace VoxPet.Core.Models;

public enum ReactionPreset { Conversation, SoftVoice, Snappy }

public static class ReactionPresets
{
    public static AudioSettings Create(ReactionPreset preset) => preset switch
    {
        ReactionPreset.Conversation => new(),
        ReactionPreset.SoftVoice => new(-65, -65, -20, 1.2, 50, 180),
        ReactionPreset.Snappy => new(-45, -45, -10, 1, 15, 80),
        _ => throw new ArgumentOutOfRangeException(nameof(preset))
    };
}
