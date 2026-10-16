using VoxPet.Core.Character;

namespace VoxPet.Core.Legacy;

/// <summary>선택한 원본에서 분리하는 여섯 부위. 의상과 같은 어깨/발 Anchor를 쓴다.</summary>
public static class CompanionParts
{
    public static IReadOnlyList<string> Ids { get; } = Array.AsReadOnly(new[] { "arm-left", "leg-left", "leg-right", "body", "head", "arm-right" });
    public static string Asset(string id) => "parts/" + id + ".png";
    public static string Head(int mouth, int eye) => $"parts/head-{mouth}-{eye}.png";
    public static string Anchor(string id) => id switch
    {
        "arm-left" => "leftShoulder",
        "arm-right" => "rightShoulder",
        "leg-left" => "leftFoot",
        "leg-right" => "rightFoot",
        "head" => "head",
        _ => "body"
    };
    public static int Order(string id) => id switch
    { "arm-left" => 10, "leg-left" => 20, "leg-right" => 30, "body" => 40, "head" => 80, _ => 100 };
    public static IReadOnlyList<CharacterPart> Define() => Ids.Select(id => new CharacterPart(id,
        id.StartsWith("arm-", StringComparison.Ordinal) ? "arm" : id.StartsWith("leg-", StringComparison.Ordinal) ? "leg" : id,
        id == "head" ? Head(0, 1) : Asset(id), Anchor(id), Order(id))).ToArray();

    // Public numeric contours only. They contain no personal pixels. The head owns
    // the original eyes/mouth; all other regions are disjoint cuts of that frame.
    public static string HeadContour(bool side) => side ? "M0,0 H512 V264 H0 Z" : "M0,0 H512 V285 H0 Z";
    public static string Contour(string id, bool side) => (side, id) switch
    {
        (false, "arm-left") => "M110,285 C85,304 65,350 64,384 C61,408 75,423 91,426 C94,446 105,458 122,457 L139,440 L146,397 C142,369 149,337 161,315 Z",
        (false, "arm-right") => "M402,285 C427,304 447,350 448,384 C451,408 437,423 421,426 C418,446 407,458 390,457 L373,440 L366,397 C370,369 363,337 351,315 Z",
        (false, "leg-left") => "M146,444 L245,444 L241,512 L146,512 Z",
        (false, "leg-right") => "M267,444 L366,444 L366,512 L271,512 Z",
        (true, "arm-left") => "M0,0 Z",
        (true, "arm-right") => "M318,264 C320,252 348,254 365,269 C382,285 402,305 420,329 C441,354 460,386 455,401 C451,414 429,420 409,416 C388,416 365,402 348,384 C330,366 314,344 309,323 C306,301 310,278 318,264 Z",
        (true, "leg-left") => "M184,410 L310,410 L310,512 L184,512 Z",
        (true, "leg-right") => "M310,410 L410,410 L410,512 L310,512 Z",
        _ => throw new ArgumentException("분리할 신체 부위가 없습니다.", nameof(id))
    };
}
