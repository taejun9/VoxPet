using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

/// <summary>입 임계값, 레벨/이동량 상한과 음성에 독립적인 blink/idle을 검증한다.</summary>
public sealed class CharacterTests
{
    [Theory]
    [InlineData(0, MouthState.Closed)]
    [InlineData(.19999, MouthState.Closed)]
    [InlineData(.2, MouthState.Half)]
    [InlineData(.59999, MouthState.Half)]
    [InlineData(.6, MouthState.Open)]
    [InlineData(1, MouthState.Open)]
    // 0.2와 0.6의 정확한 경계를 포함한 경우를 확인한다.
    public void MouthThresholds(double level, MouthState expected)
    {
        var state = new CharacterAnimator(new Random(1)).Update(level, 0);
        Assert.Equal(expected, state.Mouth); Assert.InRange(state.MouthOpen, 0, 1); Assert.InRange(state.BodyBounce, 0, 6);
    }
    [Fact]
    public void HugeFiniteTimeDoesNotOverflowAnimation()
    {
        var state = new CharacterAnimator().Update(1, double.MaxValue);
        Assert.True(double.IsFinite(state.BodyBounce)); Assert.InRange(state.BodyBounce, 0, 6);
    }
    [Fact]
    // 같은 Random seed로 30초를 진행해 무음에서도 이동/눈감음이 있고 상태가 재현되는지 검사한다.
    public void BlinkAndIdleContinueInSilenceAndAreReproducible()
    {
        var a = new CharacterAnimator(new Random(3)); var b = new CharacterAnimator(new Random(3));
        int blink = 0; double lastBounce = -1; bool moves = false;
        for (int i = 0; i < 1800; i++)
        {
            var state = a.Update(0, i / 60.0); Assert.Equal(state, b.Update(0, i / 60.0));
            if (state.EyeOpen == 0) blink++;
            if (lastBounce >= 0 && lastBounce != state.BodyBounce) moves = true;
            lastBounce = state.BodyBounce;
        }
        Assert.True(blink > 0); Assert.True(moves);
    }
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1)]
    [InlineData(20)]
    public void InvalidOrOutOfRangeLevelsAreSafe(double level)
    {
        var state = new CharacterAnimator().Update(level, 1); Assert.InRange(state.MouthOpen, 0, 1); Assert.InRange(state.BodyBounce, 0, 6);
    }
}
