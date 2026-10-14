using VoxPet.Core.Actions;
using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Motion;
using VoxPet.Core.Packages;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class NativeGeometryTests
{
    private static VoxPetPackage Package(bool head = true)
    {
        var action = new ActionDefinition("move", "move", 1000,
            new[] { new ActionTimelineEvent(0, ActionEventKind.Transform, "root", transform: new(x: 20)) }
                .Concat(head ? [new(0, ActionEventKind.Transform, "head", transform: new(x: 30))] : []));
        var cloth = new AttachmentDefinition("cloth", "cloth", "top", [
            new("moving", "cloth.png", placement: new(deformWithRig: true)),
            new("half", "half.png", placement: new(follow: .5, deformWithRig: true)),
            new("fixed", "fixed.png", placement: new(follow: 0, deformWithRig: true)),
            new("filtered", "filtered.png", inherit: new(false, false, false, false), placement: new(deformWithRig: true))]);
        var definition = new CharacterDefinition([new("body", "body", "body.png", "root")], [new("root"), new("head", 128, 128, "root")],
            actions: [action], attachments: [cloth], motion: new MotionProfile(blinkEnabled: false, idleEnabled: false, bodyBounce: 0, headBounce: 0, headRotation: 0));
        byte[] bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "ImageSetTemplate", "body.png"));
        return new VoxPetPackageValidator().Validate(new("native-mesh", "native", "1", actions: ["actions/move.json"], attachments: ["attachments/cloth.json"]), definition,
            definition.Assets.ToDictionary(p => p, _ => bytes));
    }
    [Fact]
    public void PackageRuntimeProvidesGeometryAndIndependentFollowInheritance()
    {
        var package = Package(); byte[] archive = new VoxPetPackageExporter().Export(package);
        var loaded = new VoxPetPackageLoader().Load(new MemoryStream(archive)); var runtime = loaded.CreateRuntime();
        runtime.Attachments.SetEnabled("cloth", true); runtime.RequestAction("move"); var frame = runtime.Update(new(1, 1, 0));
        var moving = frame.Layers.Single(l => l.Asset == "cloth.png"); Assert.NotNull(moving.Mesh); Assert.Equal(128, moving.Mesh!.Triangles.Count);
        var triangle = moving.Mesh.Triangles.First(t => t.A.X == 148 && t.A.Y == 128);
        var point = triangle.Matrix.Map(triangle.A.X, triangle.A.Y); Assert.Equal(178, point.X, 8); Assert.Equal(128, point.Y, 8);
        Assert.Equal(20, moving.Matrix.OffsetX); // root 이동이 mesh에 중복으로 더해지지 않는다.
        var half = frame.Layers.Single(l => l.Asset == "half.png"); var halfTriangle = half.Mesh!.Triangles.First(t => t.A.X == 138 && t.A.Y == 128);
        Assert.Equal(153, halfTriangle.Matrix.Map(138, 128).X, 8);
        Assert.Null(frame.Layers.Single(l => l.Asset == "fixed.png").Mesh); Assert.Null(frame.Layers.Single(l => l.Asset == "filtered.png").Mesh);
        runtime.Update(new(2.1, 1.1, 0)); Assert.Null(runtime.Frame.Layers.Single(l => l.Asset == "cloth.png").Mesh);
        Assert.Equal(128, moving.Mesh.Triangles.Count); // 이전 snapshot은 유지한다.
    }
    [Fact]
    public void UniformRootTransformUsesOneAffineWithoutExtraDeformation()
    {
        var runtime = Package(false).CreateRuntime(); runtime.Attachments.SetEnabled("cloth", true); runtime.RequestAction("move");
        var frame = runtime.Update(new(1, 1, 0)); var moving = frame.Layers.Single(l => l.Asset == "cloth.png");
        Assert.Null(moving.Mesh); Assert.Equal(20, moving.X); Assert.Equal(20, frame.Layers.Single(l => l.Asset == "body.png").X);
    }
}
