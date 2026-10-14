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
}
