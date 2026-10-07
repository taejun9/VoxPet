using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

/// <summary>슬롯 재시작·손상·저장 실패와 입력 독립 모션의 실제 계약을 시험한다.</summary>
public sealed class ExpressionTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "VoxPet-expressions-" + Guid.NewGuid());
    [Fact]
    public void TwelveDefaultsIncludeCryingAndIndependentBlink()
    {
        var store = new ExpressionSlotStore(folder);
        for (int i = 0; i < 12; i++) { var profile = store.Load(i); profile.Validate(); Assert.True(profile.Blink); }
        Assert.True(store.Load(2).Tears); Assert.Equal(ExpressionKind.Sad, store.Load(2).Kind);
        Assert.Throws<ArgumentOutOfRangeException>(() => store.Load(12));
    }
    [Fact]
    public void SettingsAndOwnedImageSurviveRestartWithoutSourcePath()
    {
        var store = new ExpressionSlotStore(folder);
        var bytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        var profile = new ExpressionProfile("우는 고양이", ExpressionKind.Sad, false, true, .3, .7, .4);
        var saved = store.Save(11, profile, bytes);
        var restarted = new ExpressionSlotStore(folder);
        Assert.Equal(saved, restarted.Load(11)); Assert.Equal(bytes, restarted.ReadSheet(saved));
        Assert.Equal(profile, saved with { SheetId = null });
        Assert.Single(Directory.GetFiles(folder, "*.png")); Assert.Single(Directory.GetFiles(folder, "*.json"));
        Assert.DoesNotContain("SourcePath", File.ReadAllText(Path.Combine(folder, "slot-12.json")));
    }
    [Fact]
    public void ReplacingCustomWithBuiltinRemovesOnlyManagedImage()
    {
        var store = new ExpressionSlotStore(folder);
        var first = store.Save(0, ExpressionProfile.Default(0), new byte[100]);
        var other = store.Save(1, ExpressionProfile.Default(1), new byte[120]);
        store.Save(0, ExpressionProfile.Default(0));
        Assert.False(File.Exists(Path.Combine(folder, first.SheetId + ".png")));
        Assert.Equal(new byte[120], store.ReadSheet(other)); Assert.Null(store.Load(0).SheetId);
    }
    [Theory]
    [InlineData("bad json")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"Name\":\"test\",\"Kind\":900}")]
    [InlineData("{\"Name\":\"test\",\"TearTop\":2}")]
    [InlineData("{\"Name\":\"test\",\"SheetId\":\"../../private\"}")]
    public void OneCorruptSlotDoesNotResetOtherSlots(string json)
    {
        var store = new ExpressionSlotStore(folder); var valid = new ExpressionProfile("기쁨", ExpressionKind.Happy);
        store.Save(0, valid); File.WriteAllText(Path.Combine(folder, "slot-3.json"), json);
        Assert.Equal(ExpressionProfile.Default(2), store.Load(2)); Assert.Equal(valid, store.Load(0));
    }
    [Fact]
    public void OversizedSlotAndInvalidSavePreservePrevious()
    {
        var store = new ExpressionSlotStore(folder); var original = store.Save(0, ExpressionProfile.Default(0));
        Assert.Throws<ArgumentException>(() => store.Save(0, original with { TearLeft = double.NaN }));
        Assert.Throws<ArgumentException>(() => store.Save(0, original, new byte[16 * 1024 * 1024 + 1]));
        Assert.Equal(original, store.Load(0));
        File.WriteAllText(Path.Combine(folder, "slot-2.json"), new string('x', 5000));
        Assert.Equal(ExpressionProfile.Default(1), store.Load(1));
    }
    [Fact]
    public void FailedAtomicCommitCleansNewImageAndLeavesExistingSlot()
    {
        var store = new ExpressionSlotStore(folder); store.Save(1, ExpressionProfile.Default(1), new byte[100]);
        Directory.CreateDirectory(Path.Combine(folder, "slot-1.json"));
        Assert.Throws<IOException>(() => store.Save(0, ExpressionProfile.Default(0), new byte[100]));
        Assert.Single(Directory.GetFiles(folder, "*.png")); Assert.Empty(Directory.GetFiles(folder, "*.tmp"));
        Assert.NotNull(store.Load(1).SheetId);
    }
    [Fact]
    public void TearsRemainFiniteAndPeriodicWithoutVoice()
    {
        var beginning = ExpressionMotion.Tear(.35);
        var repeated = ExpressionMotion.Tear(1.75);
        Assert.Equal(beginning.Offset, repeated.Offset, 10); Assert.Equal(beginning.Opacity, repeated.Opacity, 10);
        Assert.NotEqual(beginning, ExpressionMotion.Tear(.6));
        foreach (double seconds in new[] { 0, .1, 1.4, 100000.0, double.MaxValue, double.NaN, double.PositiveInfinity, -1 })
        {
            var motion = ExpressionMotion.Tear(seconds);
            Assert.InRange(motion.Offset, 0, 70); Assert.InRange(motion.Opacity, 0, 1);
        }
    }
    [Fact]
    public void TransitionSettlesByElapsedTimeAndHasSmoothEndpoints()
    {
        Assert.Equal(0, ExpressionMotion.Blend(0)); Assert.Equal(.5, ExpressionMotion.Blend(.12), 10);
        Assert.Equal(1, ExpressionMotion.Blend(.24)); Assert.Equal(1, ExpressionMotion.Blend(20));
        double previous = 0;
        for (int i = 0; i <= 24; i++) { double next = ExpressionMotion.Blend(i * .01); Assert.InRange(next, previous, 1); previous = next; }
        Assert.True(ExpressionMotion.Blend(.001) < .001);
        Assert.Equal(0, ExpressionMotion.Blend(double.NaN)); Assert.Equal(0, ExpressionMotion.Blend(-1));
    }
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
