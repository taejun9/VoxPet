using System.IO;
using System.Text.Json;
using VoxPet.Core.Models;

namespace VoxPet.App.Services;

public sealed record UserSettings(AudioSettings Audio, bool Topmost = true, bool GreenBackground = true);

/// <summary>Stores adjustments only; never device IDs, PCM, or microphone content.</summary>
public sealed class SettingsStore(string? settingsPath = null)
{
    private readonly string path = settingsPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VoxPet", "settings.json");
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
