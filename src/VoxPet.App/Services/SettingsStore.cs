using System.IO;
using System.Text.Json;
using VoxPet.Core.Models;

namespace VoxPet.App.Services;

/// <summary>
/// 영구 저장 대상은 오디오 조정과 방송창 옵션뿐이다. 마이크·개인 PNG·음소거는 포함하지 않는다.
/// </summary>
public sealed record UserSettings(AudioSettings Audio, bool Topmost = true, bool GreenBackground = true);

/// <summary>로컬 JSON 설정 저장소. 경로 주입으로 테스트를 실제 사용자 설정과 분리한다.</summary>
public sealed class SettingsStore(string? settingsPath = null)
{
    private readonly string path = settingsPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VoxPet", "settings.json");
    /// <summary>
    /// 파일 없음·16KiB 초과·손상·접근 오류·잘못된 수치는 기본값으로 복구해 앱 시작을 막지 않는다.
    /// </summary>
    public UserSettings Load()
    {
        try
        {
            if (!File.Exists(path)) return new(new());
            if (new FileInfo(path).Length > 16384) return new(new());
            var settings = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path));
            if (settings?.Audio == null) return new(new());
            settings.Audio.Validate();
            return settings;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        { return new(new()); }
    }
    /// <summary>
    /// 임시 파일에 완성한 JSON을 쓴 뒤 같은 경로의 기존 파일을 교체한다.
    /// 쓰기 실패는 false로 알리고 예외로 앱 종료를 막지 않는다.
    /// </summary>
    public bool Save(UserSettings settings)
    {
        try
        {
            settings.Audio.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(path + ".tmp", path, true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { return false; }
    }
}
