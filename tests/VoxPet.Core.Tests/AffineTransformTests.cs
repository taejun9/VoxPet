using VoxPet.Core.Character;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class AffineTransformTests
{
    [Fact]
    public void ParentCompositionMatchesSequentialMappingWithNonuniformScaleAndRotation()
    {
        var parent = AffineTransform2D.FromTransform(new(20, 30, 2, 3, 40));
        var local = AffineTransform2D.FromTransform(new(5, -2, .5, 1.4, -25));
        var expectedLocal = local.Map(7, 11); var expected = parent.Map(expectedLocal.X, expectedLocal.Y);
        var composed = parent.Compose(local); var actual = composed.Map(7, 11);
        Assert.Equal(expected.X, actual.X, 10); Assert.Equal(expected.Y, actual.Y, 10);
        var roundTrip = composed.Inverse().Map(actual.X, actual.Y);
        Assert.Equal(7, roundTrip.X, 10); Assert.Equal(11, roundTrip.Y, 10);
    }
    [Fact]
    public void ReflectionAndHugeFiniteRotationRemainFinite()
    {
        var reflected = AffineTransform2D.FromTransform(new(scaleX: -1));
        Assert.Equal(new RigPoint(-2, 3), reflected.Map(2, 3));
        var rotated = AffineTransform2D.FromTransform(new(rotation: double.MaxValue));
        Assert.True(double.IsFinite(rotated.Map(2, 3).X));
        var huge = new AffineTransform2D(1e200, 0, 0, 1e200);
        Assert.Equal(1e-200, huge.Inverse().M11);
    }
    [Fact]
    public void SingularNonfiniteAndOverflowingResultsAreRejected()
    {
        Assert.Throws<InvalidOperationException>(() => new AffineTransform2D(1, 1, 1, 1).Inverse());
        Assert.Throws<ArgumentOutOfRangeException>(() => new AffineTransform2D(m11: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AffineTransform2D(offsetY: double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RigPoint(0, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AffineTransform2D(1e200).Compose(new(1e200)));
    }
}
