using System.Text.Json;
using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

public sealed class RuntimePreferencesStore(string path)
{
    public RuntimePreferences Load()
    {
        try
        {
            if (!File.Exists(path) || new FileInfo(path).Length > 16384) return RuntimePreferences.Default();
            var value = JsonSerializer.Deserialize<RuntimePreferences>(File.ReadAllText(path), ImageSet.JsonOptions) ?? RuntimePreferences.Default(); value.Validate(); return value;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException) { return RuntimePreferences.Default(); }
    }
    public bool Save(RuntimePreferences value)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            value.Validate(); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(temp, JsonSerializer.Serialize(value, ImageSet.JsonOptions)); File.Move(temp, path, true); return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) { return false; }
        finally { try { File.Delete(temp); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { } }
    }
}
