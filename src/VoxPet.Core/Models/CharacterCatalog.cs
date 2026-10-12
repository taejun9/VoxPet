namespace VoxPet.Core.Models;

/// <summary>사용자 경로를 저장하지 않는 고정 캐릭터 목록. 기존 개인 기본 파일명도 유지한다.</summary>
public sealed record CharacterDefinition(string Id, string Name, string FileName, bool SideView = false);

public static class CharacterCatalog
{
    public const string DefaultId = "neulbo";
    public static IReadOnlyList<CharacterDefinition> All { get; } = Array.AsReadOnly<CharacterDefinition>([
        new(DefaultId, "늘보군", "늘보군.png"),
        new("drawing-neulbo", "그림 늘보군", "그림 늘보군.png"),
        new("neulbo-man", "늘보맨", "늘보맨.png"),
        new("neulbo-girl", "늘보걸", "늘보걸.png"),
        new("pelly", "펠리", "펠리.png", true)
    ]);
    public static string NormalizeId(string? id) => All.Any(item => item.Id == id) ? id! : DefaultId;
}
