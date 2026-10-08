using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class CalibrationTests
{
    private static NoiseGateCalibration Samples(double dbfs, int count = 150, double interval = .02)
    {
        var calibration = new NoiseGateCalibration();
        for (int i = 0; i < count; i++) calibration.AddSample(dbfs, i * interval);
        return calibration;
    }
    [Theory]
    [InlineData(-60, -54)]
    [InlineData(-63.2, -57)]
    [InlineData(-100, -80)]
    [InlineData(-21, -15)]
    public void StableNoiseGetsFiniteRecommendation(double dbfs, double expected)
    {
        Assert.True(Samples(dbfs).TryRecommend(out double gate)); Assert.Equal(expected, gate);
    }
    [Fact]
    public void BriefLoudOutliersDoNotDominateRecommendation()
    {
        var calibration = Samples(-60);
        for (int i = 0; i < 10; i++) calibration.AddSample(-10, 3 + i * .02);
        Assert.True(calibration.TryRecommend(out double gate)); Assert.Equal(-54, gate);
    }
    [Theory]
    [InlineData(-120)]
    [InlineData(-119)]
    [InlineData(-20)]
    [InlineData(0)]
    public void NoSignalAndExcessiveNoiseDoNotRecommend(double dbfs) => Assert.False(Samples(dbfs).TryRecommend(out _));
    [Fact]
    public void TooFewOrTooShortSamplesDoNotRecommend()
    {
        Assert.False(Samples(-60, 29, .1).TryRecommend(out _));
        Assert.False(Samples(-60, 150, .001).TryRecommend(out _));
    }
    [Fact]
    public void DuplicateReversedAndInvalidSamplesAreIgnored()
    {
        var calibration = new NoiseGateCalibration(); calibration.AddSample(-60, 5);
        foreach (double time in new[] { 5, 4, double.NaN, double.PositiveInfinity }) calibration.AddSample(-60, time);
        foreach (double db in new[] { double.NaN, double.NegativeInfinity, -121, 1 }) calibration.AddSample(db, 6);
        Assert.Equal(1, calibration.Count); Assert.False(calibration.TryRecommend(out _));
        calibration.AddSample(-60, 6); Assert.Equal(2, calibration.Count);
    }
    [Fact]
    public void NumericWorkingSetIsBounded()
    {
        var calibration = Samples(-60, 10000);
        Assert.Equal(512, calibration.Count); Assert.True(calibration.TryRecommend(out _));
    }
}
