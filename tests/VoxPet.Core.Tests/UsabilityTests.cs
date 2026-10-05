using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class UsabilityTests
{
    [Fact]
    public void SoftVoicePresetRespondsToQuietInputThatDefaultGatesOut()
    {
        var quiet = ReactionPresets.Create(ReactionPreset.SoftVoice); quiet.Validate();
        var normal = ReactionPresets.Create(ReactionPreset.Conversation); normal.Validate();
        Assert.Equal(0, new AudioLevelProcessor().Update(-55, 1, normal).VoiceLevel);
        Assert.True(new AudioLevelProcessor().Update(-55, 1, quiet).VoiceLevel > .2);
    }
    [Fact]
    public void SnappyPresetOpensAndClosesFaster()
    {
        var quick = ReactionPresets.Create(ReactionPreset.Snappy); quick.Validate();
        var standard = ReactionPresets.Create(ReactionPreset.Conversation);
        // Compare the time constants with identical target levels.
        standard = standard with { NoiseGate = quick.NoiseGate, NormalizeMin = quick.NormalizeMin };
        var a = new AudioLevelProcessor(); var b = new AudioLevelProcessor();
        Assert.True(a.Update(-10, .02, quick).VoiceLevel > b.Update(-10, .02, standard).VoiceLevel);
        a.Update(-10, 10, quick); b.Update(-10, 10, standard);
        Assert.True(a.Update(-120, .05, quick).VoiceLevel < b.Update(-120, .05, standard).VoiceLevel);
        Assert.Equal(new AudioSettings(), ReactionPresets.Create(ReactionPreset.Conversation));
    }
    [Theory]
    [InlineData(384, 256, 128)]
    [InlineData(1536, 1024, 512)]
    [InlineData(3072, 2048, 1024)]
    public void SheetSupportsBoundedSquareCells(int width, int height, int cell) => Assert.Equal(cell, SpriteSheetLayout.Validate(width, height));
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-384, -256)]
    [InlineData(1535, 1024)]
    [InlineData(1536, 1023)]
    [InlineData(1024, 1536)]
    [InlineData(300, 200)]
    [InlineData(3075, 2050)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void SheetRejectsMalformedAndOversizedLayouts(int width, int height) => Assert.Throws<ArgumentException>(() => SpriteSheetLayout.Validate(width, height));
}
