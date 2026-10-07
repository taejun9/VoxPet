using System.Text.Json;
using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

/// <summary>슬롯별 원자적 JSON과 관리되는 PNG 복사본. 원본 경로·마이크 데이터는 저장하지 않는다.</summary>
public sealed class ExpressionSlotStore(string folder)
{
    private string SlotPath(int slot)
    {
        if (slot is < 0 or >= 12) throw new ArgumentOutOfRangeException(nameof(slot));
        return Path.Combine(folder, $"slot-{slot + 1}.json");
    }
    /// <summary>슬롯 JSON이 없거나 4KiB를 넘거나 손상·접근 오류가 있으면 해당 슬롯의 기본값으로 복구한다.</summary>
    public ExpressionProfile Load(int slot)
    {
        string path = SlotPath(slot);
        try
        {
            if (!File.Exists(path)) return ExpressionProfile.Default(slot);
            using var file = File.OpenRead(path);
            if (file.Length > 4096) return ExpressionProfile.Default(slot);
            var bytes = new byte[(int)file.Length]; file.ReadExactly(bytes);
            var profile = JsonSerializer.Deserialize<ExpressionProfile>(bytes);
            if (profile == null) return ExpressionProfile.Default(slot);
            profile.Validate(); return profile;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        { return ExpressionProfile.Default(slot); }
    }
    /// <summary>검증한 GUID의 16MB 이하 관리 복사본을 읽는다. PNG 내용 검증은 App의 시트 로더가 수행한다.</summary>
    public byte[] ReadSheet(ExpressionProfile profile)
    {
        profile.Validate();
        if (profile.SheetId == null) throw new ArgumentException("저장된 시트가 없습니다.");
        using var file = File.OpenRead(Path.Combine(folder, profile.SheetId + ".png"));
        if (file.Length is < 33 or > 16 * 1024 * 1024) throw new ArgumentException("시트 크기를 확인하세요.");
        var bytes = new byte[(int)file.Length]; file.ReadExactly(bytes); return bytes;
    }
    /// <summary>이미지를 먼저 완성하고 JSON을 마지막에 교체한다. 실패하면 이전 슬롯이 유지된다.</summary>
    public ExpressionProfile Save(int slot, ExpressionProfile profile, byte[]? sheet = null)
    {
        string path = SlotPath(slot);
        profile.Validate();
        if (sheet != null && sheet.Length is < 33 or > 16 * 1024 * 1024) throw new ArgumentException("시트 크기를 확인하세요.");
        Directory.CreateDirectory(folder);
        var old = Load(slot);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        string? image = null;
        try
        {
            if (sheet != null)
            {
                profile = profile with { SheetId = Guid.NewGuid().ToString("N") };
                image = Path.Combine(folder, profile.SheetId + ".png");
                File.WriteAllBytes(image, sheet);
            }
            File.WriteAllText(temporary, JsonSerializer.Serialize(profile));
            File.Move(temporary, path, true);
        }
        catch
        {
            TryDelete(temporary); if (image != null) TryDelete(image); throw;
        }
        if (old.SheetId != null && old.SheetId != profile.SheetId)
            TryDelete(Path.Combine(folder, old.SheetId + ".png"));
        return profile;
    }
    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
