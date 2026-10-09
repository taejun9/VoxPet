using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class ExpressionOrderTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "VoxPet-order-qa-" + Guid.NewGuid());
    [Fact]
    public void TwelveDefaultsAreDistinctAndOldValuesRemainStable()
    {
        var defaults = Enumerable.Range(0, 12).Select(ExpressionProfile.Default).ToArray();
        Assert.Equal(12, defaults.Select(p => p.Kind).Distinct().Count());
        Assert.Equal(12, defaults.Select(p => p.Name).Distinct().Count());
        Assert.Equal(2, (int)ExpressionKind.Sad); Assert.Equal(5, (int)ExpressionKind.Sleepy);
        Assert.All(defaults, p => p.Validate());
    }
    [Fact]
    public void ReorderSurvivesRestartWithoutTouchingSlotOrPng()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "slot-1.json"), "owned-slot");
        File.WriteAllBytes(Path.Combine(folder, "owned.png"), [1, 2, 3]);
        var order = Enumerable.Range(0, 12).Reverse().ToArray(); new ExpressionOrderStore(folder).Save(order);
        Assert.Equal(order, new ExpressionOrderStore(folder).Load());
        Assert.Equal("owned-slot", File.ReadAllText(Path.Combine(folder, "slot-1.json")));
        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(folder, "owned.png")));
    }
    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("[0,0,2,3,4,5,6,7,8,9,10,11]")]
    [InlineData("[0,1,2,3,4,5,6,7,8,9,10,12]")]
    [InlineData("bad json")]
    public void CorruptOrderRecoversToIdentity(string value)
    {
        Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, "order.json"), value);
        Assert.Equal(Enumerable.Range(0, 12), new ExpressionOrderStore(folder).Load());
    }
    [Fact]
    public void InvalidSaveKeepsOldOrderAndFailedCommitCleansTemporary()
    {
        var store = new ExpressionOrderStore(folder); var order = Enumerable.Range(0, 12).Reverse().ToArray(); store.Save(order);
        Assert.Throws<ArgumentException>(() => store.Save(new int[12])); Assert.Equal(order, store.Load());
        File.Delete(Path.Combine(folder, "order.json")); Directory.CreateDirectory(Path.Combine(folder, "order.json"));
        Assert.NotNull(Record.Exception(() => store.Save(order)));
        Assert.Empty(Directory.GetFiles(folder, "*.tmp"));
    }
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
