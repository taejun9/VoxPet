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
    // 선택 ID만 로컬 저장하며 PCM·측정값·장치 이름·PNG 경로는 저장하지 않는다.
    public void SaveAndReloadPreservesLastSettings()
    {
        var settings = new UserSettings(new(NoiseGate: -35, NormalizeMin: -75, NormalizeMax: -15, Sensitivity: 1.7, AttackMs: 50, ReleaseMs: 200), false, false, "pelly",
            "QA-endpoint-id", new(920, 680, -1100, 140, true), new(610, 420, 110, 160), true, true, 6, 8);
        var store = new SettingsStore(FilePath); Assert.True(store.Save(settings)); Assert.Equal(settings, store.Load());
        var json = File.ReadAllText(FilePath);
        Assert.Contains("QA-endpoint-id", json); Assert.DoesNotContain("DeviceName", json); Assert.DoesNotContain("Pcm", json); Assert.DoesNotContain("AudioLevel", json);
        Assert.DoesNotContain(".png", json);
    }
    [Fact]
    public void CorruptNewFieldsRecoverIndependently()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(FilePath, "{\"Audio\":{\"Sensitivity\":1.7},\"MicrophoneId\":\"bad\\nID\",\"SettingsTab\":42,\"ExpressionSlotId\":100,\"BuiltinExpression\":100,\"MainWindow\":{\"Width\":-1,\"Height\":600,\"Left\":2000000,\"Top\":-1600}}");
        var loaded = new SettingsStore(FilePath).Load();
        Assert.Equal(1.7, loaded.Audio.Sensitivity); Assert.Null(loaded.MicrophoneId); Assert.Equal(0, loaded.SettingsTab);
        Assert.Null(loaded.ExpressionSlotId); Assert.Null(loaded.BuiltinExpression);
        Assert.Equal(new WindowPlacement(1000, 600, null, -1600), loaded.MainWindow);
    }
    [Fact]
    public void BuiltinAppearanceRoundTripsWithoutChangingSlotData()
    {
        var store = new SettingsStore(FilePath); var settings = new UserSettings(new(), BuiltinExpression: ExpressionKind.Happy);
        Assert.True(store.Save(settings)); Assert.Equal(settings, store.Load());
    }
    [Theory]
    [InlineData("null")]
    [InlineData("\"unknown\"")]
    [InlineData("\"../private.png\"")]
    public void InvalidCharacterPreservesOtherSettings(string id)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(FilePath, "{\"Audio\":{\"Sensitivity\":1.7},\"Topmost\":false,\"CharacterId\":" + id + "}");
        var loaded = new SettingsStore(FilePath).Load();
        Assert.Equal(CharacterCatalog.DefaultId, loaded.CharacterId); Assert.Equal(1.7, loaded.Audio.Sensitivity); Assert.False(loaded.Topmost);
    }
    [Fact]
    public void LegacySettingsUseExistingCharacter()
    {
        Directory.CreateDirectory(folder); File.WriteAllText(FilePath, "{\"Audio\":{\"Sensitivity\":1.7}}");
        var loaded = new SettingsStore(FilePath).Load();
        Assert.Equal(CharacterCatalog.DefaultId, loaded.CharacterId); Assert.Equal(1.7, loaded.Audio.Sensitivity);
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
