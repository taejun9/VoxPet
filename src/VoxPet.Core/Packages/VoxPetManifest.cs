using VoxPet.Core.Character;

namespace VoxPet.Core.Packages;

/// <summary>패키지 메타데이터. 규격 버전과 제작자가 지정한 캐릭터 버전은 독립적이다.</summary>
public sealed record VoxPetManifest
{
    public string Format => "voxpet";
    public int FormatVersion { get; }
    public string Id { get; }
    public string Name { get; }
    public string Version { get; }
    public string Author { get; }
    public string Description { get; }
    public string Character { get; }
    public string? Thumbnail { get; }
    public IReadOnlyList<string> Actions { get; }
    public IReadOnlyList<string> Attachments { get; }

    public VoxPetManifest(string id, string name, string version, string author = "", string description = "",
        string character = "character.json", string? thumbnail = null, IEnumerable<string>? actions = null,
        IEnumerable<string>? attachments = null, int formatVersion = 2)
    {
        if (formatVersion != 2) throw new ArgumentOutOfRangeException(nameof(formatVersion));
        FormatVersion = formatVersion;
        Id = DefinitionValidation.Id(id, nameof(id));
        Name = DefinitionValidation.Text(name, nameof(name));
        Version = DefinitionValidation.Text(version, nameof(version));
        ArgumentNullException.ThrowIfNull(author);
        ArgumentNullException.ThrowIfNull(description);
        Author = author;
        Description = description;
        Character = DefinitionValidation.Asset(character, ".json", nameof(character));
        Thumbnail = thumbnail is null ? null : DefinitionValidation.Asset(thumbnail, ".png", nameof(thumbnail));
        Actions = References(actions ?? [], nameof(actions));
        Attachments = References(attachments ?? [], nameof(attachments));
    }

    private static IReadOnlyList<string> References(IEnumerable<string> references, string parameter)
    {
        var copy = DefinitionValidation.Copy(references, parameter);
        foreach (string path in copy) DefinitionValidation.Asset(path, ".json", parameter);
        if (copy.Distinct(StringComparer.OrdinalIgnoreCase).Count() != copy.Count)
            throw new ArgumentException("문서 참조가 중복됩니다.", parameter);
        return copy;
    }
}
