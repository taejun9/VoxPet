using VoxPet.Core.Attachments;
using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class AttachmentCompatibilityTests
{
    [Fact]
    public void CostumeAdapterPreservesMouthFollowLogicalSizeAndLayerOrder()
    {
        var profile = new CostumeProfile(Guid.NewGuid(), "콧수염", CostumeCategory.FaceDecoration, 120, 210, 140, 60, 1.3,
            Attachment: MotionAttachment.Mouth, Follow: .5);
        var attachment = LegacyCostumeAdapter.Convert(profile, "wardrobe/mustache.png", 3); var layer = attachment.Layers.Single();
        Assert.Equal(profile.Id.ToString("N"), attachment.Id); Assert.Equal("faceDecoration", attachment.Category);
        Assert.Equal("mouth", layer.Anchor); Assert.True(layer.Inherit.MouthMotion);
        Assert.Equal(new Transform2D(120, 210, 1.3, 1.3), layer.Transform);
        Assert.Equal(140, layer.Placement.Width); Assert.Equal(60, layer.Placement.Height); Assert.Equal(.5, layer.Placement.Follow);
        Assert.Equal(1003, layer.ZIndex); Assert.Equal(AttachmentSpace.Canvas, layer.Placement.Space);
        var garment = LegacyCostumeAdapter.Convert(profile with { Category = CostumeCategory.Top, Attachment = MotionAttachment.Auto }, "shirt.png");
        Assert.True(garment.Layers[0].Placement.DeformWithRig);
        var behind = LegacyCostumeAdapter.Convert(profile with { BehindCharacter = true }, "mustache.png", 2);
        Assert.Equal(-998, behind.Layers[0].ZIndex);
    }
    [Fact]
    public void LegacyWardrobeAndOpaqueImageBytesRemainReadableAndUnmodified()
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-attachment-" + Guid.NewGuid());
        try
        {
            var profile = new CostumeProfile(Guid.NewGuid(), "saved", CostumeCategory.Hat, 100, 20, 200, 80);
            var legacy = new CostumeStore(folder); byte[] opaqueImage = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
            legacy.Import([profile], profile.Id, opaqueImage);
            byte[] before = File.ReadAllBytes(Path.Combine(folder, "wardrobe.json"));
            var store = new AttachmentStore(folder); var inventory = store.LoadLegacy();
            Assert.False(inventory.Recovered); Assert.Equal(profile.Id.ToString("N"), inventory.EnabledIds.Single());
            Assert.Equal("head", inventory.Definitions[0].Layers[0].Anchor);
            Assert.Equal(opaqueImage, store.ReadLegacyImage(profile.Id)); Assert.Equal(before, File.ReadAllBytes(Path.Combine(folder, "wardrobe.json")));
            File.WriteAllText(Path.Combine(folder, "wardrobe.json"), "broken");
            Assert.True(store.LoadLegacy().Recovered); Assert.Equal("broken", File.ReadAllText(Path.Combine(folder, "wardrobe.json")));
            Assert.Equal(opaqueImage, store.ReadLegacyImage(profile.Id));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    [Fact]
    public void PlacementAndInventoryRejectNonfiniteSizesAndBrokenSelections()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AttachmentTransform(follow: double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AttachmentTransform(width: double.PositiveInfinity, height: 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AttachmentTransform(width: 10, height: 0));
        Assert.Throws<ArgumentException>(() => new AttachmentTransform(width: 10));
        Assert.Throws<ArgumentException>(() => new AttachmentLayer("hat", "hat.png", transform: new(1, 2), placement: new(new(3, 4))));
        Assert.Throws<ArgumentException>(() => new AttachmentInventory([], ["missing"]));
    }
}
