using System.Buffers.Binary;
using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class AudioTests
{
    private static byte[] FloatBuffer(params float[] samples)
    {
        byte[] result = new byte[samples.Length * 4];
        for (int i = 0; i < samples.Length; i++) BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(i * 4), BitConverter.SingleToInt32Bits(samples[i]));
        return result;
    }
    private static AudioLevel Float(params float[] samples) => AudioAnalyzer.Analyze(FloatBuffer(samples), new(SampleEncoding.Float, 32, 1));
    [Fact]
    public void EmptyAndSilenceAreFinite()
    {
        Assert.Equal(AudioLevel.Silence, Float()); Assert.Equal(AudioLevel.Silence, Float(0, 0, 0));
    }
    [Fact]
    public void FullScaleAndClipping()
    {
        Assert.Equal(new AudioLevel(1, 1, 0), Float(1, -1));
        var clipped = Float(2, -2); Assert.Equal(2, clipped.Peak); Assert.Equal(6.0206, clipped.Dbfs, 4);
    }
    [Fact]
    public void SineHasExpectedRms()
    {
        var sine = Enumerable.Range(0, 48000).Select(i => (float)Math.Sin(i * Math.PI * 2 * 1000 / 48000)).ToArray();
        var level = Float(sine); Assert.Equal(Math.Sqrt(.5), level.Rms, 6); Assert.Equal(-3.0103, level.Dbfs, 4);
    }
    [Fact]
    public void OppositePhaseChannelsDoNotCancel()
    {
        var level = AudioAnalyzer.Analyze(FloatBuffer(1, -1, 1, -1), new(SampleEncoding.Float, 32, 2));
        Assert.Equal(1, level.Rms);
    }
    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public void SignedPcmDecodesNegativeFullScale(int bits)
    {
        byte[] bytes = new byte[bits / 8];
        if (bits != 8) bytes[^1] = 128; // unsigned 8-bit zero is -1
        Assert.Equal(1, AudioAnalyzer.Analyze(bytes, new(SampleEncoding.Pcm, bits, 1)).Rms);
    }
    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public void PcmPositiveAndSilence(int bits)
    {
        byte[] bytes = new byte[bits / 8];
        if (bits == 8) bytes[0] = 128;
        Assert.Equal(AudioLevel.Silence, AudioAnalyzer.Analyze(bytes, new(SampleEncoding.Pcm, bits, 1)));
        Array.Fill(bytes, (byte)255); if (bits != 8) bytes[^1] = 127;
        double expected = 1 - Math.Pow(2, 1 - bits);
        Assert.Equal(expected, AudioAnalyzer.Analyze(bytes, new(SampleEncoding.Pcm, bits, 1)).Rms, 9);
    }
    [Fact]
    public void Pcm24SignExtensionForMinusHalf()
    {
        Assert.Equal(.5, AudioAnalyzer.Analyze([0, 0, 192], new(SampleEncoding.Pcm, 24, 1)).Rms);
    }
    [Fact]
    public void Float64AndInvalidSamples()
    {
        byte[] bytes = new byte[8]; BinaryPrimitives.WriteInt64LittleEndian(bytes, BitConverter.DoubleToInt64Bits(-.5));
        Assert.Equal(.5, AudioAnalyzer.Analyze(bytes, new(SampleEncoding.Float, 64, 1)).Rms);
        Assert.Equal(AudioLevel.Silence, Float(float.NaN, float.PositiveInfinity, float.NegativeInfinity));
        Assert.True(double.IsFinite(Float(float.MaxValue).Dbfs));
    }
    [Theory]
    [InlineData(12, 1)]
    [InlineData(16, 0)]
    [InlineData(16, 33)]
    public void RejectsUnsupportedPcm(int bits, int channels) => Assert.Throws<NotSupportedException>(() => AudioAnalyzer.Analyze([], new(SampleEncoding.Pcm, bits, channels)));
    [Fact]
    public void RejectsPartialSampleAndFrame()
    {
        Assert.Throws<ArgumentException>(() => AudioAnalyzer.Analyze([0], new(SampleEncoding.Pcm, 16, 1)));
        Assert.Throws<ArgumentException>(() => AudioAnalyzer.Analyze([0, 0], new(SampleEncoding.Pcm, 16, 2)));
        Assert.Throws<NotSupportedException>(() => AudioAnalyzer.Analyze([], new((SampleEncoding)999, 16, 1)));
    }
    [Fact]
    public void GateUsesOriginalDbAndIncludesBoundary()
    {
        var settings = new AudioSettings(NoiseGate: -40, Sensitivity: 4, AttackMs: 0);
        var p = new AudioLevelProcessor();
        Assert.Equal(0, p.Update(-40.01, .02, settings).VoiceLevel);
        Assert.Equal(1, p.Update(-40, .02, settings).VoiceLevel);
        Assert.Equal(1, p.Update(-39, .02, settings).VoiceLevel);
    }
    [Theory]
    [InlineData(-120, 0)]
    [InlineData(-50, 0)]
    [InlineData(-30, .5)]
    [InlineData(-10, 1)]
    [InlineData(3, 1)]
    public void NormalizationAndClamp(double db, double expected)
    {
        Assert.Equal(expected, new AudioLevelProcessor().Update(db, .01, new(AttackMs: 0)).Raw);
    }
    [Fact]
    public void QuietInputCanReactWithAdjustedNormalizationRange()
    {
        var p = new AudioLevelProcessor();
        Assert.Equal(0, p.Update(-65, .1, new(NoiseGate: -70, AttackMs: 0)).VoiceLevel);
        Assert.Equal(.25, p.Update(-65, .1, new(NoiseGate: -70, NormalizeMin: -80, NormalizeMax: -20, AttackMs: 0)).VoiceLevel);
    }
    [Fact]
    public void ReleaseIsPeriodIndependent()
    {
        var a = new AudioLevelProcessor(); var b = new AudioLevelProcessor(); var s = new AudioSettings(AttackMs: 0);
        a.Update(0, .01, s); b.Update(0, .01, s);
        for (int i = 0; i < 60; i++) a.Update(-120, .14 / 60, s);
        b.Update(-120, .14, s); Assert.Equal(b.VoiceLevel, a.VoiceLevel, 12);
    }
    [Fact]
    public void SensitivityCannotBypassGateAndZeroIsMute()
    {
        var p = new AudioLevelProcessor();
        Assert.Equal(0, p.Update(-60, 1, new(Sensitivity: 10)).VoiceLevel);
        Assert.Equal(0, p.Update(0, 1, new(Sensitivity: 0)).VoiceLevel);
    }
    [Fact]
    public void AttackReleaseUseTimeConstants()
    {
        var p = new AudioLevelProcessor(); var s = new AudioSettings();
        double first = p.Update(0, .04, s).VoiceLevel;
        Assert.Equal(1 - Math.Exp(-1), first, 12);
        double second = p.Update(0, .04, s).VoiceLevel; Assert.True(second > first);
        double third = p.Update(-120, .14, s).VoiceLevel; Assert.Equal(second * Math.Exp(-1), third, 12);
        Assert.True(third < second);
    }
    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void SmoothingIsPeriodIndependent(int hz)
    {
        var p = new AudioLevelProcessor(); var reference = new AudioLevelProcessor();
        for (int i = 0; i < hz; i++) p.Update(-10, 1.0 / hz, new());
        Assert.Equal(reference.Update(-10, 1, new()).VoiceLevel, p.VoiceLevel, 12);
    }
    [Fact]
    public void ZeroTimeZeroTauSettlingAndReset()
    {
        var p = new AudioLevelProcessor();
        Assert.Equal(0, p.Update(-10, 0, new(AttackMs: 0)).VoiceLevel);
        Assert.Equal(1, p.Update(-10, .01, new(AttackMs: 0)).VoiceLevel);
        Assert.Equal(0, p.Update(-120, .01, new(ReleaseMs: 0)).VoiceLevel);
        p.Update(-10, .5, new()); p.Update(-120, 10, new()); Assert.Equal(0, p.VoiceLevel);
        p.Update(-10, .5, new()); p.Reset(); Assert.Equal(0, p.VoiceLevel);
    }
    [Fact]
    public void RejectsBadSettingsAndTime()
    {
        AudioSettings[] settings = [new(NormalizeMin: 0, NormalizeMax: 0), new(Sensitivity: -1), new(Sensitivity: double.NaN), new(NoiseGate: double.PositiveInfinity), new(AttackMs: -1), new(ReleaseMs: double.NaN), new(NormalizeMin: -double.MaxValue, NormalizeMax: double.MaxValue)];
        foreach (var s in settings) Assert.Throws<ArgumentOutOfRangeException>(() => new AudioLevelProcessor().Update(0, .01, s));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AudioLevelProcessor().Update(0, double.NaN, new()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AudioLevelProcessor().Update(0, -1, new()));
        Assert.Equal(0, new AudioLevelProcessor().Update(double.NaN, .1, new()).VoiceLevel);
        Assert.Equal(1, new AudioLevelProcessor().Update(double.MaxValue, 1, new(AttackMs: 0)).VoiceLevel);
    }
    [Fact]
    public void SyntheticUtteranceRemainsBoundedAndReturnsToSilence()
    {
        var p = new AudioLevelProcessor();
        double[] db = [.. Enumerable.Repeat(-120.0, 60), .. Enumerable.Repeat(-25.0, 8), .. Enumerable.Repeat(-15.0, 60), .. Enumerable.Repeat(-120.0, 180)];
        foreach (var v in db) { var output = p.Update(v, 1.0 / 60, new()); Assert.InRange(output.VoiceLevel, 0, 1); Assert.True(double.IsFinite(output.VoiceLevel)); }
        Assert.Equal(0, p.VoiceLevel);
    }
    [Fact]
    public void FuzzLevelsAndIntervalsAreFinite()
    {
        var r = new Random(7); var p = new AudioLevelProcessor();
        for (int i = 0; i < 10000; i++) Assert.InRange(p.Update(r.NextDouble() * 400 - 200, r.NextDouble(), new(Sensitivity: r.NextDouble() * 4)).VoiceLevel, 0, 1);
    }
}
