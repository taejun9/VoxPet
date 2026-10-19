namespace VoxPet.Core.Models;

/// <summary>사용자 경로를 저장하지 않는 고정 캐릭터 목록. 기존 개인 기본 파일명도 유지한다.</summary>
public sealed record CharacterDefinition(string Id, string Name, string FileName, bool SideView = false,
    string Description = "", string Personality = "", string CostumeRecommendation = "");

public static class CharacterCatalog
{
    public const string DefaultId = "neulbo";
    public static IReadOnlyList<CharacterDefinition> All { get; } = Array.AsReadOnly<CharacterDefinition>([
        new(DefaultId, "늘보군", "늘보군.png", false,
            "느긋하고 사랑스러운 나무늘보 친구",
            "여유로운 호흡과 포근하고 부드러운 반응",
            "모자, 셔츠, 코트, 바지, 신발, 칼/활 (정면 의상 최적화)"),
        new("pelly", "펠리", "펠리.png", true,
            "활기차고 똑똑한 펠리컨 친구",
            "경쾌한 호흡과 즉각적인 부리/날개 반응",
            "산책 조끼, 니트 조끼, 망토, 날개 맞춤 총/마법봉 (측면 의상 최적화)")
    ]);
    public static string NormalizeId(string? id) => All.Any(item => item.Id == id) ? id! : DefaultId;
    public static CharacterDefinition? Find(string? id) => All.FirstOrDefault(item => item.Id == id);
    public static CharacterDefinition Current(string? id) => Find(NormalizeId(id)) ?? All[0];
}
