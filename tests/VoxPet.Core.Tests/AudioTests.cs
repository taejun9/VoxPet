using System.Buffers.Binary;
using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

/// <summary>실제 마이크 없이 PCM 디코딩, 수학적 기준값, Gate 경계와 시간 독립성을 검증한다.</summary>
public sealed class AudioTests
{
    // 합성 float를 Windows PCM과 같은 little-endian 바이트 배열로 만들어 디코딩 경로까지 시험한다.
    private static byte[] FloatBuffer(params float[] samples)
    {
        byte[] result = new byte[samples.Length * 4];
        for (int i = 0; i < samples.Length; i++) BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(i * 4), BitConverter.SingleToInt32Bits(samples[i]));
        return result;
    }
    // 합성 float를 Windows PCM과 같은 little-endian 바이트 배열로 만들어 디코딩 경로까지 시험한다.
    private static AudioLevel Float(params float[] samples) => AudioAnalyzer.Analyze(FloatBuffer(samples), new(SampleEncoding.Float, 32, 1));
    [Fact]
    public void EmptyAndSilenceAreFinite()
    {
        Assert.Equal(AudioLevel.Silence, Float()); Assert.Equal(AudioLevel.Silence, Float(0, 0, 0));
    }
    [Fact]
    // full scale은 0 dBFS이고 2배 입력은 약 +6.0206 dBFS다. 진단값을 1로 잘라서는 안 된다.
    public void FullScaleAndClipping()
    {
        Assert.Equal(new AudioLevel(1, 1, 0), Float(1, -1));
        var clipped = Float(2, -2); Assert.Equal(2, clipped.Peak); Assert.Equal(6.0206, clipped.Dbfs, 4);
    }
    [Fact]
    // 1kHz full-scale 사인파의 RMS는 1/√2, dBFS는 약 -3.0103이다.
    public void SineHasExpectedRms()
    {
        var sine = Enumerable.Range(0, 48000).Select(i => (float)Math.Sin(i * Math.PI * 2 * 1000 / 48000)).ToArray();
        var level = Float(sine); Assert.Equal(Math.Sqrt(.5), level.Rms, 6); Assert.Equal(-3.0103, level.Dbfs, 4);
    }
    [Fact]
    // 좌우 채널 값의 단순 평균은 0이 되지만 채널별 제곱 에너지는 유지되어야 한다.
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
        if (bits != 8) bytes[^1] = 128; // unsigned 8비트의 0은 정규화하면 -1이다.
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
    // 24비트 0xC00000은 -0.5다. 부호 확장 없이 양수로 해석하는 회귀를 잡는다.
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
    // Gate 바로 아래는 닫히고 Gate와 같은 값은 통과한다. 높은 감도도 원본 dBFS 판정을 바꾸지 않는다.
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
    // 같은 총 경과 시간을 여러 번 나누거나 한 번에 적용해도 Release 결과가 같아야 한다.
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
    // 시정수 한 번 동안 증가분은 1-e^-1, 감소 후 잔여는 e^-1이다. ms를 완료 시간으로 취급하지 않는다.
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
    // 10/30/60/120Hz 모두 동일한 1초 누적 결과를 가져야 한다.
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
    // 무음 → 짧은 음절 → 유지 발화 → 무음 순서를 숫자 배열로 재현한다.
    public void SyntheticUtteranceRemainsBoundedAndReturnsToSilence()
    {
        var p = new AudioLevelProcessor();
        double[] db = [.. Enumerable.Repeat(-120.0, 60), .. Enumerable.Repeat(-25.0, 8), .. Enumerable.Repeat(-15.0, 60), .. Enumerable.Repeat(-120.0, 180)];
        foreach (var v in db) { var output = p.Update(v, 1.0 / 60, new()); Assert.InRange(output.VoiceLevel, 0, 1); Assert.True(double.IsFinite(output.VoiceLevel)); }
        Assert.Equal(0, p.VoiceLevel);
    }
    [Fact]
    // 고정 seed의 다양한 입력으로 결과의 유한한 0~1 계약을 반복 확인한다.
    public void FuzzLevelsAndIntervalsAreFinite()
    {
        var r = new Random(7); var p = new AudioLevelProcessor();
        for (int i = 0; i < 10000; i++) Assert.InRange(p.Update(r.NextDouble() * 400 - 200, r.NextDouble(), new(Sensitivity: r.NextDouble() * 4)).VoiceLevel, 0, 1);
    }
}
