using System.Text.Json;

namespace VoxPet.Core.Services;

/// <summary>표시/단축키 위치를 저장 슬롯ID에 매핑하는 원자적 순열. PNG·슬롯 JSON은 변경하지 않는다.</summary>
public sealed class ExpressionOrderStore(string folder)
{
    private readonly string path = Path.Combine(folder, "order.json");
    public static void Validate(IReadOnlyList<int> order)
    {
        if (order.Count != 12 || order.Any(i => i is < 0 or >= 12) || order.Distinct().Count() != 12)
            throw new ArgumentException("표정 순서는0~11을 한 번씩 포함해야 합니다.");
    }
    public int[] Load()
    {
        try
        {
            using var file = File.OpenRead(path);
            if (file.Length > 1024) return Enumerable.Range(0, 12).ToArray();
            var order = JsonSerializer.Deserialize<int[]>(file) ?? [];
            Validate(order); return order;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        { return Enumerable.Range(0, 12).ToArray(); }
    }
    public void Save(IReadOnlyList<int> order)
    {
        var snapshot = order.ToArray(); Validate(snapshot);
        Directory.CreateDirectory(folder);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(snapshot)); File.Move(temporary, path, true); }
        finally
        {
            try { File.Delete(temporary); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }
}
