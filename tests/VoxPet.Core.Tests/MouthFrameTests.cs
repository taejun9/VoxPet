using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

/// <summary>상세8단계 경계와 dBFS부터 정규화까지 실제 반응 경로를 검증한다.</summary>
public sealed class MouthFrameTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(.019999, 0)]
    [InlineData(.02, 1)]
    [InlineData(.099999, 1)]
    [InlineData(.10, 2)]
    [InlineData(.22, 3)]
    [InlineData(.38, 4)]
    [InlineData(.56, 5)]
    [InlineData(.74, 6)]
    [InlineData(.90, 7)]
    [InlineData(1, 7)]
    [InlineData(double.NaN, 0)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(-1, 0)]
    [InlineData(20, 7)]
    public void ThresholdsAndInvalidLevels(double level, int expected) => Assert.Equal(expected, MouthFrames.Select(level));

    [Fact]
    public void DbfsSweepVisitsEveryFrameAndGateClosesMouth()
    {
        var processor = new AudioLevelProcessor();
        var settings = new AudioSettings(NoiseGate: -65, NormalizeMin: -65, NormalizeMax: -20, AttackMs: 0, ReleaseMs: 0);
        var frames = new HashSet<int>(); int previous = 0;
        for (double db = -65; db <= -20; db += .1)
        {
            processor.Update(db, .02, settings);
            int frame = MouthFrames.Select(processor.VoiceLevel);
            Assert.InRange(frame, previous, MouthFrames.Count - 1); previous = frame; frames.Add(frame);
        }
        Assert.Equal(MouthFrames.Count, frames.Count);
        processor.Update(-63, .02, settings);
        Assert.Equal(1, MouthFrames.Select(processor.VoiceLevel));
        processor.Update(-66, .02, settings);
        Assert.Equal(0, MouthFrames.Select(processor.VoiceLevel));
    }

    [Theory]
    [InlineData(1024, 256, 128)]
    [InlineData(2048, 512, 256)]
    [InlineData(4096, 1024, 512)]
    public void DetailedSheetSizes(int width, int height, int cell)
    {
        Assert.Equal(cell, SpriteSheetLayout.Validate(width, height));
        Assert.Equal(MouthFrames.Count, SpriteSheetLayout.Columns(width, height));
    }
    [Theory]
    [InlineData(8192, 2048)]
    [InlineData(2048, 513)]
    [InlineData(1792, 512)]
    public void DetailedSheetBounds(int width, int height) => Assert.Throws<ArgumentException>(() => SpriteSheetLayout.Validate(width, height));
}
