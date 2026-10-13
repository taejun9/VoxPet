using VoxPet.Core.Models;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class CharacterCatalogTests
{
    [Fact]
    public void RequestedOrderAndSideViewAreStable()
    {
        Assert.Equal(new[] { "늘보군", "펠리" }, CharacterCatalog.All.Select(item => item.Name));
        Assert.Equal(2, CharacterCatalog.All.Select(item => item.Id).Distinct().Count());
        Assert.Equal("늘보군.png", CharacterCatalog.All[0].FileName);
        Assert.Single(CharacterCatalog.All, item => item.SideView);
        Assert.True(CharacterCatalog.All[1].SideView);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("../private.png")]
    [InlineData("unknown")]
    [InlineData("drawing-neulbo")]
    [InlineData("neulbo-man")]
    [InlineData("neulbo-girl")]
    public void UnknownIdsRecoverToExistingDefault(string? id) => Assert.Equal(CharacterCatalog.DefaultId, CharacterCatalog.NormalizeId(id));
}
