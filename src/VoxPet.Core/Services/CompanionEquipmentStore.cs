using System.Text.Json;
using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

public sealed class CompanionEquipmentStore(string path)
{
    public Dictionary<string, CompanionEquipment> Load()
    {
        try
        {
            if (!File.Exists(path) || new FileInfo(path).Length > 4096) return [];
            var result = JsonSerializer.Deserialize<Dictionary<string, CompanionEquipment>>(File.ReadAllText(path)) ?? [];
            Validate(result); return result;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException) { return []; }
    }
    private static void Validate(IReadOnlyDictionary<string, CompanionEquipment> choices)
    {
        if (choices.Count > 2 || choices.Any(p => p.Key is not ("neulbo" or "pelly") || p.Value == null)) throw new ArgumentException("캐릭터 무기 설정을 확인하세요.");
        foreach (var value in choices.Values) value.Validate();
    }
    public bool Save(IReadOnlyDictionary<string, CompanionEquipment> choices)
    {
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Validate(choices); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(temp, JsonSerializer.Serialize(choices)); File.Move(temp, path, true); return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) { return false; }
        finally { try { File.Delete(temp); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { } }
    }
}
