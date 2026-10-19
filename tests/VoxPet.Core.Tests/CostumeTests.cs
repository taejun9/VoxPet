using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class CostumeTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "VoxPet-costumes-" + Guid.NewGuid());
    private static CostumeProfile Profile(CostumeCategory category = CostumeCategory.Hat) => new(Guid.NewGuid(), "test", category, 12.5, 32, 120, 60);
    [Fact]
    public void ImportedBytesAndPlacementSurviveRestartAndDeleteOnlyManagedCopy()
    {
        var item = Profile(); byte[] bytes = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray(); var store = new CostumeStore(folder);
        store.Import([item], item.Id, bytes); store.Save([item with { X = -12.25, Scale = 1.73, Enabled = false }]);
        var restart = new CostumeStore(folder); var loaded = restart.Load();
        Assert.False(loaded.Recovered); Assert.Equal(-12.25, loaded.Items[0].X); Assert.Equal(1.73, loaded.Items[0].Scale); Assert.False(loaded.Items[0].Enabled);
        Assert.Equal(bytes, restart.ReadImage(item.Id)); restart.Delete([], item.Id); Assert.Empty(restart.Load().Items);
        Assert.Throws<FileNotFoundException>(() => restart.ReadImage(item.Id)); Assert.Equal(64, bytes.Length);
    }
    [Fact]
    public void InvalidSnapshotDoesNotReplacePreviousIndexOrImages()
    {
        var item = Profile(); var store = new CostumeStore(folder); store.Import([item], item.Id, new byte[64]);
        Assert.Throws<ArgumentException>(() => store.Save([item with { X = double.NaN }])); Assert.Equal(item, store.Load().Items[0]);
        Assert.Throws<ArgumentException>(() => store.Save([item, item])); Assert.Equal(64, store.ReadImage(item.Id).Length);
    }
    [Theory]
    [InlineData(CostumeCategory.OnePiece, CostumeCategory.Top, true)]
    [InlineData(CostumeCategory.Bottom, CostumeCategory.OnePiece, true)]
    [InlineData(CostumeCategory.OnePiece, CostumeCategory.Outerwear, false)]
    [InlineData(CostumeCategory.Top, CostumeCategory.Bottom, false)]
    [InlineData(CostumeCategory.FaceDecoration, CostumeCategory.FaceDecoration, false)]
    [InlineData(CostumeCategory.Earrings, CostumeCategory.Earrings, false)]
    [InlineData(CostumeCategory.Hat, CostumeCategory.Hat, true)]
    public void OutfitCategoriesHaveExpectedCoexistence(CostumeCategory first, CostumeCategory second, bool conflict) => Assert.Equal(conflict, CostumeProfile.Conflicts(first, second));
    [Fact]
    public void CorruptMetadataKeepsImageAndReportsRecovery()
    {
        var item = Profile(); var store = new CostumeStore(folder); store.Import([item], item.Id, new byte[64]);
        File.WriteAllText(Path.Combine(folder, "wardrobe.json"), "bad"); Assert.True(store.Load().Recovered); Assert.Empty(store.Load().Items); Assert.Equal(64, store.ReadImage(item.Id).Length);
    }
    [Fact]
    public void FailedIndexCommitRollsBackNewImageAndTemporaryFiles()
    {
        Directory.CreateDirectory(Path.Combine(folder, "wardrobe.json")); var item = Profile(); var store = new CostumeStore(folder);
        Assert.NotNull(Record.Exception(() => store.Import([item], item.Id, new byte[64]))); Assert.Empty(Directory.GetFiles(folder));
    }
    [Fact]
    public void CharacterSpecificSuitabilityAndDefaultPlacement()
    {
        var item = Profile() with { TargetCharacter = "neulbo" };
        Assert.True(item.FitsCharacter("neulbo"));
        Assert.False(item.FitsCharacter("pelly"));
        Assert.True(new CostumeProfile(Guid.NewGuid(), "universal", CostumeCategory.Hat, 0, 0, 100, 100).FitsCharacter("pelly"));

        var neulboPlacement = CostumeProfile.DefaultPlacement(CostumeCategory.Hat, "neulbo");
        var pellyPlacement = CostumeProfile.DefaultPlacement(CostumeCategory.Hat, "pelly");
        Assert.True(double.IsFinite(neulboPlacement.X));
        Assert.True(double.IsFinite(pellyPlacement.X));
        Assert.NotEqual(neulboPlacement.X, pellyPlacement.X);

        Assert.Throws<ArgumentException>(() => (Profile() with { TargetCharacter = "invalid" }).Validate());
    }
    [Fact]
    public void FormalSuitAndDressPresetsAndMatchingAccessories()
    {
        var suit = CostumeProfile.CreateSuit("neulbo");
        var dress = CostumeProfile.CreateDress("pelly");
        var topHat = CostumeProfile.CreateTopHat("neulbo");
        var bowtie = CostumeProfile.CreateBowtie("neulbo");
        var tiara = CostumeProfile.CreateTiara("pelly");
        var pearl = CostumeProfile.CreatePearlSet("pelly");

        Assert.Equal(CostumeCategory.OnePiece, suit.Category);
        Assert.Equal(CostumeCategory.OnePiece, dress.Category);
        Assert.Equal(CostumeCategory.Hat, topHat.Category);
        Assert.Equal(CostumeCategory.FaceDecoration, bowtie.Category);
        Assert.Equal(CostumeCategory.Hat, tiara.Category);
        Assert.Equal(CostumeCategory.Earrings, pearl.Category);

        Assert.True(CostumeProfile.Conflicts(suit.Category, CostumeCategory.Top));
        Assert.True(CostumeProfile.Conflicts(suit.Category, CostumeCategory.Bottom));
        Assert.False(CostumeProfile.Conflicts(suit.Category, topHat.Category));
        Assert.False(CostumeProfile.Conflicts(suit.Category, bowtie.Category));
        Assert.False(CostumeProfile.Conflicts(dress.Category, tiara.Category));
        Assert.False(CostumeProfile.Conflicts(dress.Category, pearl.Category));
        Assert.False(CostumeProfile.Conflicts(dress.Category, CostumeCategory.Outerwear));

        suit.Validate();
        dress.Validate();
        topHat.Validate();
        bowtie.Validate();
        tiara.Validate();
        pearl.Validate();
    }
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
