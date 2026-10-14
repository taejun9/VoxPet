using VoxPet.Core.Character;
using VoxPet.Core.Motion;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class MotionEngineTests
{
    [Fact]
    public void OptionalHeadMotionAndSilenceHaveIndependentContributions()
    {
        var engine = new MotionEngine([new VoiceMotion(1, 5, 2, 1.5), new IdleMotion(.4, .2)]);
        var silent = engine.Update(0, Math.PI / 4); Assert.Equal(0, silent.MouthOpen);
        Assert.Equal(-.4, silent.Transforms["body"].Y, 10); Assert.Equal(0, silent.Transforms["head"].Y);
        var voice = engine.Update(1, Math.PI / 4); Assert.Equal(1, voice.MouthOpen);
        Assert.Equal(-5.4, voice.Transforms["body"].Y, 10); Assert.Equal(-2, voice.Transforms["head"].Y);
        Assert.Equal(1.5, voice.Transforms["head"].Rotation); Assert.NotEqual(0, voice.Transforms["body"].X);
    }
    [Fact]
    public void LegacyWrapperPreservesFormulaAndSeededBlinkSchedule()
    {
        var referenceRandom = new Random(7); var actual = new CharacterAnimator(new Random(7));
        double nextBlink = 2 + referenceRandom.NextDouble() * 4, blinkUntil = 0;
        for (int i = 0; i < 3600; i++)
        {
            double time = i / 60d, level = i % 4 / 3d;
            if (time >= nextBlink) { blinkUntil = time + .14; nextBlink = blinkUntil + 2 + referenceRandom.NextDouble() * 4; }
            var value = actual.Update(level, time);
            Assert.Equal(Math.Min(6, .5 + .5 * Math.Sin((time % Math.PI) * 2) + level * 5), value.BodyBounce, 12);
            Assert.Equal(time < blinkUntil ? 0 : 1, value.EyeOpen); Assert.Equal(level, value.MouthOpen);
        }
    }
    [Fact]
    public void BlinkBoundariesAndDelayedFramesDoNotQueueMissedBlinks()
    {
        var blink = new BlinkMotion(new Random(1), 2, 2, .14);
        Assert.Equal(1, blink.Update(new(1.99, 0, 0)).EyeOpen);
        Assert.Equal(0, blink.Update(new(2, .01, 0)).EyeOpen);
        Assert.Equal(1, blink.Update(new(2.14, .14, 0)).EyeOpen);
        Assert.Equal(0, blink.Update(new(100, 97.86, 0)).EyeOpen);
        Assert.Equal(1, blink.Update(new(100.2, .2, 0)).EyeOpen);
    }
    [Fact]
    public void DeterministicInputsProduceIdenticalFullPoses()
    {
        var a = MotionEngine.Legacy(new Random(3)); var b = MotionEngine.Legacy(new Random(3));
        for (int i = 0; i < 1800; i++)
        {
            var first = a.Update(i % 10 / 10d, i / 60d, 1 / 60d); var second = b.Update(i % 10 / 10d, i / 60d, 1 / 60d);
            Assert.Equal(first.MouthOpen, second.MouthOpen); Assert.Equal(first.EyeOpen, second.EyeOpen);
            Assert.Equal(first.Transforms.OrderBy(p => p.Key), second.Transforms.OrderBy(p => p.Key));
        }
        Assert.True(double.IsFinite(a.Update(1, double.MaxValue).Transforms["body"].Y));
    }
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-1)]
    public void InvalidTimeAndSourceSettingsAreRejected(double value)
    {
        var engine = MotionEngine.Legacy();
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Update(0, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Update(0, 0, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VoiceMotion(mouth: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VoiceMotion(bodyBounce: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VoiceMotion(headBounce: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new VoiceMotion(headRotation: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IdleMotion(breathing: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new IdleMotion(sway: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BlinkMotion(intervalMin: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BlinkMotion(intervalMax: value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BlinkMotion(duration: value));
    }
    [Fact]
    public void SourcesAreCopiedAndInputValidationIsShared()
    {
        IMotionSource[] sources = [new VoiceMotion()]; var engine = new MotionEngine(sources); sources[0] = new VoiceMotion(0, 0);
        Assert.Equal(1, engine.Update(1, 0).MouthOpen);
        Assert.Empty(new MotionEngine([]).Update(1, 0).Transforms);
        Assert.Throws<ArgumentException>(() => new MotionEngine([null!]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BlinkMotion(intervalMin: 3, intervalMax: 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Update(new MotionInput(0, 0, double.NaN)));
        Assert.Equal(0, engine.Update(double.NaN, 0).MouthOpen); Assert.Equal(1, engine.Update(20, 0).MouthOpen);
    }
    [Fact]
    public void CompositionKeepsBlinkClosureAndAddsCustomTransformSources()
    {
        var engine = new MotionEngine([new Constant(new(.2, .6, new Dictionary<string, Transform2D> { ["tail"] = new(2, 3, 2, 2, 10) })),
            new Constant(new(.5, 0, new Dictionary<string, Transform2D> { ["tail"] = new(4, 5, .5, 1, -2) }))]);
        var result = engine.Update(0, 0); Assert.Equal(.5, result.MouthOpen); Assert.Equal(0, result.EyeOpen);
        Assert.Equal(new Transform2D(6, 8, 1, 2, 8), result.Transforms["tail"]);
    }
    private sealed class Constant(CharacterPose pose) : IMotionSource
    {
        public CharacterPose Update(MotionInput input) => pose;
    }
}
