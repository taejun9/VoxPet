using System.Text.Json;
using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

public sealed class HandMotionStore(string path)
{
    public (HandMotionSettings Settings, bool Recovered) Load()
    {
        if (!File.Exists(path)) return (HandMotionSettings.Default(), false);
        try
        {
            using var file = File.OpenRead(path); if (file.Length > 32 * 1024) throw new ArgumentException("설정 크기");
            var settings = JsonSerializer.Deserialize<HandMotionSettings>(file) ?? throw new ArgumentException("빈 설정");
            settings.Validate(); return (settings, false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException) { return (HandMotionSettings.Default(), true); }
    }
    public bool Save(HandMotionSettings settings)
    {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            settings.Validate(); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings)); File.Move(temporary, path, true); return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) { return false; }
        finally { try { File.Delete(temporary); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { } }
    }
}
