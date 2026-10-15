using VoxPet.Core.Attachments;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class StudioSampleTests
{
    private static VoxPetPackage Sample() => new VoxPetPackageLoader().Load(Path.Combine(AppContext.BaseDirectory, "Samples", "studio-samples.voxpet"));
    [Fact]
    public void ShippedSampleCoversEveryCostumeGestureAndActionTemplateWithoutGlobalInputs()
    {
        var package = Sample();
        Assert.Equal("voxpet.samples.studio", package.Manifest.Id);
        Assert.Equal(AttachmentSystem.Categories.Order(), package.Character.Attachments.Select(a => a.Category).Order());
        Assert.Equal(14, package.Character.Attachments.Count); Assert.Equal(9, package.Character.Actions.Count);
        foreach (var template in Enum.GetNames<ActionTemplate>()) Assert.Contains(package.Character.Actions, a => a.Name.Contains(template));
        foreach (string id in new[] { "claw", "toy-gun", "heart", "toy-sword", "magic-wand" }) Assert.Contains(package.Character.Actions, a => a.Id == id);
        Assert.All(package.Character.Actions.SelectMany(a => a.Triggers), t => Assert.Equal(VoxPet.Core.Triggers.TriggerKind.Manual, t.Kind));
        var roundTrip = new VoxPetPackageLoader().Load(new MemoryStream(new VoxPetPackageExporter().Export(package)));
        foreach (string asset in package.AssetPaths) Assert.Equal(package.ReadAsset(asset), roundTrip.ReadAsset(asset));
    }
    [Fact]
    public void EveryCostumeCanBeWornRemovedAndCombinedUsingRealConflictRules()
    {
        var package = Sample(); var runtime = package.CreateRuntime(new Random(1));
        foreach (var item in package.Character.Attachments)
        {
            runtime.Attachments.SetEnabled(item.Id, true); var frame = runtime.Update(new(0, 0, 0, false));
            Assert.Contains(item.Id, runtime.State.AttachmentIds);
            Assert.Contains(frame.Layers, l => l.Asset == item.Layers[0].Asset);
            runtime.Attachments.SetEnabled(item.Id, false);
            Assert.DoesNotContain(runtime.Update(new(0, 0, 0, false)).Layers, l => l.Asset == item.Layers[0].Asset);
        }
        runtime.Attachments.SetEnabled("top", true); runtime.Attachments.SetEnabled("bottom", true); runtime.Attachments.SetEnabled("onePiece", true);
        Assert.DoesNotContain("top", runtime.Attachments.EnabledIds); Assert.DoesNotContain("bottom", runtime.Attachments.EnabledIds);
        runtime.Attachments.SetEnabled("hat", true); runtime.Attachments.SetEnabled("outerwear", true);
        Assert.Contains("onePiece", runtime.Attachments.EnabledIds); Assert.Contains("hat", runtime.Attachments.EnabledIds); Assert.Contains("outerwear", runtime.Attachments.EnabledIds);
    }
    [Fact]
    public void AllNineActionsMoveTheRigShowTheirPropAndReturnToIdleWithClothes()
    {
        var package = Sample();
        foreach (var action in package.Character.Actions)
        {
            var runtime = package.CreateRuntime(new Random(1)); runtime.Attachments.SetEnabled("top", true); runtime.Attachments.SetEnabled("handAccessory", true);
            runtime.Update(new(0, 0, 0, false)); var rest = runtime.CurrentRig.Animated["rightHand"];
            runtime.RequestAction(action.Id); runtime.Update(new(0, 0, 0, false)); var playing = runtime.Update(new(.2, .2, 0, false));
            Assert.Equal(action.Id, runtime.State.ActionId); Assert.NotEqual(rest, runtime.CurrentRig.Animated["rightHand"]);
            Assert.Contains(playing.Layers, l => l.Id.Contains("effect"));
            Assert.Contains(playing.Layers, l => l.Asset == "samples/top.png" && l.Mesh != null);
            Assert.All(playing.Layers, l => { Assert.True(double.IsFinite(l.X)); Assert.True(double.IsFinite(l.Y)); });
            runtime.Update(new(1.2, 1, 0, false)); Assert.Null(runtime.State.ActionId);
        }
    }
}
