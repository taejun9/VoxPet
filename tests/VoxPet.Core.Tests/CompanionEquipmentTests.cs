using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class CompanionEquipmentTests
{
    private static VoxPetPackage Load(string id) => new VoxPetPackageLoader().Load(Path.Combine(AppContext.BaseDirectory, "Samples", "companion-" + id + ".voxpet"));
    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void WeaponSelectionAndHoldAreIndependentOfActionLifetime(string id)
    {
        var package = Load(id); var processor = new LegacySpriteSheetRuntime { Samples = package.Character, Rig = CharacterRig.Default(id) };
        var runtime = new CharacterRuntime(LegacySpriteSheetAdapter.Convert(3, samples: package.Character), processor: processor);
        double time = 0;
        foreach (var weapon in CompanionWeapon.All)
        {
            processor.WeaponId = weapon.Id; processor.HoldWeapon = true;
            Assert.Equal(weapon.Asset, runtime.Update(new(time, 0, 0)).Layers.Single(l => l.Id == "companion-equipped-weapon").Asset);
            processor.HoldWeapon = false;
            Assert.DoesNotContain(runtime.Update(new(time, 0, 0)).Layers, l => l.Id == "companion-equipped-weapon");
            runtime.RequestAction(weapon.Id); runtime.Update(new(time, 0, 0));
            var active = runtime.Update(new(time + .2, .2, 0));
            Assert.Single(active.Layers, l => l.Asset == weapon.Asset);
            Assert.NotEqual(VoxPet.Core.Character.AffineTransform2D.Identity, active.Layers.Single(l => l.Id == "companion-equipped-weapon").Matrix);
            processor.WeaponId = null;
            Assert.DoesNotContain(runtime.Update(new(time + .21, .01, 0)).Layers, l => l.Id == "companion-equipped-weapon");
            runtime.StopActions(); time += 2;
            processor.WeaponId = weapon.Id; processor.HoldWeapon = true;
            runtime.RequestAction("claw"); runtime.Update(new(time, 0, 0));
            Assert.DoesNotContain(runtime.Update(new(time + .2, .2, 0)).Layers, l => l.Id == "companion-equipped-weapon");
            Assert.Contains(runtime.Update(new(time + 1.2, 1, 0)).Layers, l => l.Id == "companion-equipped-weapon"); time += 2;
        }
    }
    [Theory]
    [InlineData("neulbo")]
    [InlineData("pelly")]
    public void ClothesHaveSeparateMatchingTorsoSleevesAndFeet(string id)
    {
        var p = Load(id); var runtime = new CharacterRuntime(LegacySpriteSheetAdapter.Convert(3, samples: p.Character));
        foreach (var garment in new[] { "top", "outerwear", "onePiece" })
        {
            runtime.Attachments.SetEnabled(garment, true); runtime.Attachments.SetEnabled("shoes", true);
            runtime.RequestAction("two-hand"); runtime.Update(new(runtime.Actions.LastTime, 0, 0));
            var frame = runtime.Update(new(runtime.Actions.LastTime + .2, .2, 0));
            foreach (var side in new[] { "left", "right" })
            {
                var sleeve = frame.Layers.Single(l => l.Asset == $"samples/{garment}-{side}.png");
                var arm = frame.Layers.Single(l => l.Id == "part-arm-" + side);
                Assert.Equal(arm.Matrix, sleeve.Matrix); Assert.Null(sleeve.Mesh);
                Assert.Equal(frame.Layers.Single(l => l.Id == "part-leg-" + side).Matrix, frame.Layers.Single(l => l.Asset == $"samples/shoes-{side}.png").Matrix);
            }
            Assert.Equal(frame.Layers.Single(l => l.Id == "part-body").Matrix, frame.Layers.Single(l => l.Asset == $"samples/{garment}-body.png").Matrix);
            runtime.Attachments.SetEnabled(garment, false); runtime.StopActions();
        }
        var law = p.Character.Actions.Single(a => a.Id == "claw"); Assert.Equal("법규", law.Name);
        Assert.Contains(law.Timeline, e => e.Kind == VoxPet.Core.Actions.ActionEventKind.Effect && e.Target == "rightHand");
        Assert.Contains(law.Timeline, e => e.Kind == VoxPet.Core.Actions.ActionEventKind.Transform && e.Target == "rightHand");
    }
    [Fact]
    public void EquipmentStorePreservesBothCharactersAndRejectsInvalidReplacement()
    {
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()); Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, "equipment.json"); var store = new CompanionEquipmentStore(path);
            var choices = new Dictionary<string, CompanionEquipment> { ["neulbo"] = new("bow", false), ["pelly"] = new("toy-gun", true) };
            Assert.True(store.Save(choices)); Assert.Equal(choices["neulbo"], store.Load()["neulbo"]); Assert.Equal(choices["pelly"], store.Load()["pelly"]);
            byte[] prior = File.ReadAllBytes(path); choices["pelly"] = new("unknown", true);
            Assert.False(store.Save(choices)); Assert.Equal(prior, File.ReadAllBytes(path));
            File.WriteAllText(path, "broken"); Assert.Empty(store.Load()); Assert.Equal("broken", File.ReadAllText(path));
        }
        finally { Directory.Delete(folder, true); }
    }
}
