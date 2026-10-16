using VoxPet.Core.Character;
using VoxPet.Core.Rendering;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class PartSkinningTests
{
    [Theory]
    [InlineData(-70)]
    [InlineData(52)]
    public void ShoulderBoundaryStaysConnectedAndWristFollowsTheBone(double angle)
    {
        var delta = new AffineTransform2D(offsetX: 367, offsetY: 315).Compose(AffineTransform2D.FromTransform(new(rotation: angle))).Compose(new(offsetX: -367, offsetY: -315));
        var mesh = JointSkinning.Compose(delta, new(315, 415))!;
        foreach (var triangle in mesh.Triangles)
            foreach (var point in new[] { triangle.A, triangle.B, triangle.C })
            {
                var actual = triangle.Matrix.Map(point.X, point.Y);
                if (point.Y <= 315) { Assert.Equal(point.X, actual.X, 8); Assert.Equal(point.Y, actual.Y, 8); }
                if (point.Y >= 415) { var expected = delta.Map(point.X, point.Y); Assert.Equal(expected.X, actual.X, 8); Assert.Equal(expected.Y, actual.Y, 8); }
            }
        var groups = mesh.Triangles.SelectMany(t => new[] { (Point: t.A, t.Matrix), (Point: t.B, t.Matrix), (Point: t.C, t.Matrix) }).GroupBy(p => p.Point);
        foreach (var group in groups)
        {
            var first = group.First().Matrix.Map(group.Key.X, group.Key.Y);
            foreach (var vertex in group) { var actual = vertex.Matrix.Map(vertex.Point.X, vertex.Point.Y); Assert.Equal(first.X, actual.X, 8); Assert.Equal(first.Y, actual.Y, 8); }
        }
        Assert.Null(JointSkinning.Compose(AffineTransform2D.Identity, new(315, 415)));
    }
    [Theory]
    [InlineData(-1, 2)]
    [InlineData(4, 4)]
    [InlineData(4, 4.5)]
    [InlineData(5, 4)]
    [InlineData(0, 16385)]
    [InlineData(double.NaN, 10)]
    [InlineData(0, double.PositiveInfinity)]
    public void InvalidSkinningIsRejected(double root, double full) => Assert.ThrowsAny<ArgumentException>(() => new PartSkinning(root, full));
}
