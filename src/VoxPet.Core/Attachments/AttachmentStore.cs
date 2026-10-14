using System.Text;
using System.Text.Json;
using VoxPet.Core.Packages;
using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Services;

namespace VoxPet.Core.Attachments;

public sealed record AttachmentInventory
{
    public IReadOnlyList<AttachmentDefinition> Definitions { get; }
    public IReadOnlyList<string> EnabledIds { get; }
    public bool Recovered { get; }
    public AttachmentInventory(IEnumerable<AttachmentDefinition> definitions, IEnumerable<string> enabledIds, bool recovered = false)
    {
        Definitions = DefinitionValidation.Copy(definitions, nameof(definitions)); EnabledIds = DefinitionValidation.Copy(enabledIds, nameof(enabledIds));
        if (Definitions.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != Definitions.Count ||
            EnabledIds.Distinct(StringComparer.Ordinal).Count() != EnabledIds.Count || EnabledIds.Any(id => !Definitions.Any(d => d.Id == id)))
            throw new ArgumentException("보관함 정의/선택 참조를 확인하세요.");
        Recovered = recovered;
    }
}

/// <summary>기존 wardrobe 파일을 수정하지 않는 읽기 경계. animation tick에서 호출하지 않는다.</summary>
public sealed class AttachmentStore(string folder)
{
    private readonly CostumeStore legacy = new(folder);
    public AttachmentInventory LoadLegacy()
    {
        var loaded = legacy.Load();
        return new(loaded.Items.Select((profile, order) => LegacyCostumeAdapter.Convert(profile, $"wardrobe/{profile.Id:N}.png", order)),
            loaded.Items.Where(profile => profile.Enabled).Select(profile => profile.Id.ToString("N")), loaded.Recovered);
    }
    // 반환 데이터는 원래 PNG/JPG/BMP/GIF bytes다. PNG 참조로의 재인코딩은 App worker가 담당한다.
    public byte[] ReadLegacyImage(Guid id) => legacy.ReadImage(id);
    private string NativePath => Path.Combine(folder, "attachments-v2.json");
    public AttachmentInventory LoadNative()
    {
        if (!File.Exists(NativePath)) return new([], []);
        try
        {
            using var stream = File.OpenRead(NativePath); byte[] bytes = VoxPetPackageLoader.ReadBounded(stream, new VoxPetPackageLimits().JsonBytes);
            using var doc = PackageJson.Parse(bytes); var root = doc.RootElement;
            if (root.GetProperty("schemaVersion").GetInt32() != 2) throw new VoxPetPackageException("부착물 저장 버전 오류입니다.");
            var definitions = root.GetProperty("definitions").EnumerateArray().Select(e => PackageJson.ReadAttachment(Encoding.UTF8.GetBytes(e.GetRawText()))).ToArray();
            new AttachmentSystem(definitions);
            return new(definitions, root.GetProperty("enabledIds").EnumerateArray().Select(e => e.GetString()!));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or VoxPetPackageException or KeyNotFoundException)
        { return new([], [], true); }
    }
    public void SaveNative(AttachmentInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory); new AttachmentSystem(inventory.Definitions);
        using var memory = new MemoryStream();
        using (var writer = new Utf8JsonWriter(memory))
        {
            writer.WriteStartObject(); writer.WriteNumber("schemaVersion", 2); writer.WriteStartArray("definitions");
            foreach (var definition in inventory.Definitions) writer.WriteRawValue(PackageJson.WriteAttachment(definition));
            writer.WriteEndArray(); writer.WritePropertyName("enabledIds"); JsonSerializer.Serialize(writer, inventory.EnabledIds); writer.WriteEndObject();
        }
        if (memory.Length > new VoxPetPackageLimits().JsonBytes) throw new VoxPetPackageException("부착물 저장 크기 상한입니다.");
        Directory.CreateDirectory(folder); string temporary = NativePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(temporary, memory.ToArray()); File.Move(temporary, NativePath, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

}
