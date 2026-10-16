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
    public static IReadOnlyList<CharacterPart> Define(CharacterDefinition samples) => Ids.Select(id => new CharacterPart(id,
        id.StartsWith("arm-", StringComparison.Ordinal) ? "arm" : id.StartsWith("leg-", StringComparison.Ordinal) ? "leg" : id,
        id == "head" ? Head(0, 1) : Asset(id), Anchor(id), Order(id), skinning: samples.Parts.FirstOrDefault(p => p.Id == id)?.Skinning)).ToArray();

    // Public numeric contours only. They contain no personal pixels. The head owns
    // the original eyes/mouth; all other regions are disjoint cuts of that frame.
    public static string HeadContour(bool side) => side ? "M0,0 H512 V248 H360 C341,265 331,282 315,292 C302,299 286,297 272,287 C262,275 252,271 238,274 C225,285 208,290 184,292 H0 Z" : "M0,0 H512 V285 H0 Z";
    public static string BodyContour(bool side) => side
        ? "M230,245 C200,265 181,302 184,345 C180,397 214,427 283,430 C344,438 376,409 377,367 C376,322 358,282 337,257 Z"
        : "M150,274 C137,286 127,308 130,334 L132,400 C134,449 156,462 197,460 L228,456 Q256,449 284,456 L315,460 C356,462 379,448 380,403 L382,334 C385,309 373,286 358,274 Z";
    public static string Contour(string id, bool side) => (side, id) switch
    {
        (false, "arm-left") => "M0,315 H163 C146,352 145,394 143,415 L150,455 L140,512 H0 Z",
        (false, "arm-right") => "M349,315 H512 V512 H372 L362,455 L369,415 C367,394 366,352 349,315 Z",
        (false, "leg-left") => "M146,444 L245,444 L241,512 L146,512 Z",
        (false, "leg-right") => "M267,444 L366,444 L366,512 L271,512 Z",
        (true, "arm-left") => "M0,0 Z",
        (true, "arm-right") => "M305,264 C320,244 351,249 374,267 C409,294 444,337 465,379 C481,410 457,431 425,429 C390,426 361,408 338,386 C312,363 301,340 300,316 C298,293 300,274 305,264 Z",
        (true, "leg-left") => "M250,410 L291,410 L311,431 L291,455 L215,456 L205,442 L237,425 Z",
        (true, "leg-right") => "M307,410 L346,411 L349,429 C373,438 390,453 360,471 L301,471 L284,452 L302,429 Z",
        _ => throw new ArgumentException("분리할 신체 부위가 없습니다.", nameof(id))
    };
}
