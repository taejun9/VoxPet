using VoxPet.Core.Character;

namespace VoxPet.Core.Legacy;

/// <summary>선택한 원본에서 분리하는 여섯 부위. 의상과 같은 어깨/발 Anchor를 쓴다.</summary>
public static class CompanionParts
{
    public static IReadOnlyList<string> Ids { get; } = Array.AsReadOnly(new[] { "arm-left", "leg-left", "leg-right", "body", "head", "arm-right" });
    public static string Asset(string id) => "parts/" + id + ".png";
    public static string GripArm(string side) => "parts/arm-" + side + "-grip.png";
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
    { "arm-left" => 90, "leg-left" => 20, "leg-right" => 30, "body" => 40, "head" => 120, _ => 100 };
    public static IReadOnlyList<CharacterPart> Define(CharacterDefinition samples) => Ids.Select(id => new CharacterPart(id,
        id.StartsWith("arm-", StringComparison.Ordinal) ? "arm" : id.StartsWith("leg-", StringComparison.Ordinal) ? "leg" : id,
        id == "head" ? Head(0, 1) : Asset(id), Anchor(id), Order(id), skinning: samples.Parts.FirstOrDefault(p => p.Id == id)?.Skinning)).ToArray();

    // Pelly contours include the loader centering/baseline registration (-18,+17).
    // Public numeric contours only. They contain no personal pixels. The head owns
    // the original eyes/mouth; all other regions are disjoint cuts of that frame.
    public static string HeadContour(bool side) => side ? "M -18 17 H 494 V 250 H 342 C 323 282 313 299 297 309 C 284 316 268 314 254 304 C 244 292 234 288 220 291 C 207 302 190 307 166 309 L160,326 H -18 Z" : "M0,0 H512 V285 H0 Z";
    public static string BodyContour(bool side) => side
        ? "M 212 262 C 182 282 163 319 166 362 C 162 414 196 444 265 447 C 326 455 358 426 359 384 C 388 358 404 329 386 295 C386 275 378 260 370 250 Z"
        : "M150,274 C137,286 127,308 130,334 L132,400 C134,449 156,462 197,460 L228,456 Q256,449 284,456 L315,460 C356,462 379,448 380,403 L382,334 C385,309 373,286 358,274 Z";
    public static string NeckContour(bool side) => side ? "M212,262 Q260,240 318,245 Q358,248 376,280 L366,319 H212 Z" : BodyContour(false);
    public static string Contour(string id, bool side) => (side, id) switch
    {
        (false, "arm-left") => "M0,285 H163 C170,311 158,335 151,363 C146,389 145,407 143,415 L150,455 L140,512 H0 Z",
        (false, "arm-right") => "M349,285 H512 V512 H372 L362,455 L369,415 C367,407 366,389 361,363 C354,335 342,311 349,285 Z",
        (false, "leg-left") => "M146,460 L256,460 L256,512 L146,512 Z",
        (false, "leg-right") => "M256,460 L366,460 L366,512 L256,512 Z",
        (true, "arm-left") => "M0,0 Z",
        (true, "arm-right") => "M 280 240 H512 V452 H407 C 372 443 343 425 320 403 C 294 380 283 357 282 333 C 280 310 282 291 287 281 Z",
        (true, "leg-left") => "M228,435 H278 V463 L254,484 H180 V458 L215,443 Z",
        (true, "leg-right") => "M278,435 H330 L335,451 C360,462 360,485 342,495 H254 V484 L278,463 Z",
        _ => throw new ArgumentException("분리할 신체 부위가 없습니다.", nameof(id))
    };
}
