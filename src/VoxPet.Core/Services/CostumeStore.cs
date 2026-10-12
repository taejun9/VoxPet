using System.Text.Json;
using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

/// <summary>의상 원본의 관리 복사본과 배치를 저장한다. 외부 경로나 음성은 보관하지 않는다.</summary>
public sealed class CostumeStore(string folder)
{
    private string IndexPath => Path.Combine(folder, "wardrobe.json");
    private string ImagePath(Guid id) => id == Guid.Empty ? throw new ArgumentException("의상 ID") : Path.Combine(folder, id.ToString("N") + ".img");
    public (CostumeProfile[] Items, bool Recovered) Load()
    {
        if (!File.Exists(IndexPath)) return ([], false);
        try
        {
            using var file = File.OpenRead(IndexPath); if (file.Length > 128 * 1024) return ([], true);
            var items = JsonSerializer.Deserialize<CostumeProfile[]>(file) ?? []; Validate(items); return (items, false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException) { return ([], true); }
    }
    public static void Validate(IReadOnlyList<CostumeProfile> items)
    {
        if (items.Any(item => item == null) || items.Count > 100 || items.Select(item => item.Id).Distinct().Count() != items.Count || items.Count(item => item.Enabled) > 24)
            throw new ArgumentException("의상 보관100개/착용24개 제한 또는 중복 ID를 확인하세요.");
        foreach (var item in items) item.Validate();
    }
    public byte[] ReadImage(Guid id)
    {
        using var file = File.OpenRead(ImagePath(id));
        if (file.Length is < 16 or > 16 * 1024 * 1024) throw new ArgumentException("이미지 크기");
        var bytes = new byte[(int)file.Length]; file.ReadExactly(bytes); return bytes;
    }
    public void Import(IReadOnlyList<CostumeProfile> items, Guid id, byte[] bytes)
    {
        Validate(items);
        if (!items.Any(item => item.Id == id) || bytes.Length is < 16 or > 16 * 1024 * 1024) throw new ArgumentException("이미지 크기/ID");
        Directory.CreateDirectory(folder); string image = ImagePath(id);
        bool created = false;
        try
        {
            using (var file = new FileStream(image, FileMode.CreateNew, FileAccess.Write)) { created = true; file.Write(bytes); }
            Save(items);
        }
        catch { if (created) TryDelete(image); throw; }
    }
    public void Save(IReadOnlyList<CostumeProfile> items)
    {
        var snapshot = items.ToArray(); Validate(snapshot); Directory.CreateDirectory(folder);
        string temporary = IndexPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(snapshot)); File.Move(temporary, IndexPath, true); }
        finally { TryDelete(temporary); }
    }
    public void Delete(IReadOnlyList<CostumeProfile> remaining, Guid id)
    {
        if (remaining.Any(item => item.Id == id)) throw new ArgumentException("삭제할 ID가 목록에 남아 있습니다.");
        Save(remaining);
        TryDelete(ImagePath(id));
    }
    private static void TryDelete(string path) { try { File.Delete(path); } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { } }
}
