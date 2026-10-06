using VoxPet.App.Services;
using VoxPet.Core.Models;
using Xunit;

namespace VoxPet.Core.Tests;

/// <summary>임시 경로를 사용해 사용자 설정을 건드리지 않고 저장·손상 복구·쓰기 실패를 시험한다.</summary>
public sealed class SettingsTests : IDisposable
{
    private readonly string folder = Path.Combine(Path.GetTempPath(), "VoxPet-tests-" + Guid.NewGuid());
    private string FilePath => Path.Combine(folder, "settings.json");
    [Fact] public void MissingSettingsUseDefaults() => Assert.Equal(new UserSettings(new()), new SettingsStore(FilePath).Load());
    [Fact]
    // 왕복 저장과 함께 장치 ID·PCM·측정값 필드가 영구 파일에 포함되지 않는지 확인한다.
    public void SaveAndReloadPreservesAdjustmentsOnly()
    {
        var settings = new UserSettings(new(NoiseGate: -35, NormalizeMin: -75, NormalizeMax: -15, Sensitivity: 1.7, AttackMs: 50, ReleaseMs: 200), false, false);
        var store = new SettingsStore(FilePath); Assert.True(store.Save(settings)); Assert.Equal(settings, store.Load());
        var json = File.ReadAllText(FilePath);
        Assert.DoesNotContain("Device", json); Assert.DoesNotContain("Pcm", json); Assert.DoesNotContain("AudioLevel", json);
    }
    [Theory]
    [InlineData("bad json")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"Audio\":null}")]
    [InlineData("{\"Audio\":{\"Sensitivity\":-2}}")]
    [InlineData("{\"Audio\":{\"NormalizeMin\":0,\"NormalizeMax\":0}}")]
    public void CorruptSettingsRecover(string json)
    {
        Directory.CreateDirectory(folder); File.WriteAllText(FilePath, json);
        Assert.Equal(new UserSettings(new()), new SettingsStore(FilePath).Load());
    }
    [Fact]
    public void OversizedSettingsRecover()
    {
        Directory.CreateDirectory(folder); File.WriteAllText(FilePath, new string('x', 20000));
        Assert.Equal(new UserSettings(new()), new SettingsStore(FilePath).Load());
    }
    [Fact]
    // 파일을 디렉터리처럼 사용해 플랫폼에 의존하지 않는 쓰기 실패를 만든다.
    public void UnwritablePathDoesNotCrashOrDestroyExistingFile()
    {
        Directory.CreateDirectory(folder); File.WriteAllText(FilePath, "existing");
        var store = new SettingsStore(Path.Combine(FilePath, "settings.json"));
        Assert.False(store.Save(new(new()))); Assert.Equal("existing", File.ReadAllText(FilePath));
    }
    public void Dispose() { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}
