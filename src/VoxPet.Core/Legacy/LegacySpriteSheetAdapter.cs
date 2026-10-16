using VoxPet.Core.Character;
using VoxPet.Core.Motion;
using VoxPet.Core.Models;
using CharacterDefinition = VoxPet.Core.Character.CharacterDefinition;

namespace VoxPet.Core.Legacy;

/// <summary>full-canvas 시트는 Core 조합 정책이 opaque frame asset을 선택한다.</summary>
public static class LegacySpriteSheetAdapter
{
    public static CharacterDefinition Convert(int mouthFrames, ExpressionProfile? profile = null, CharacterDefinition? samples = null)
    {
        if (mouthFrames is not (3 or 8)) throw new ArgumentOutOfRangeException(nameof(mouthFrames));
        profile ??= ExpressionProfile.Default(0); profile.Validate(); string id = profile.Kind.ToString().ToLowerInvariant();
        string Frame(int m, int e) => samples == null ? Asset(m, e) : CompanionParts.Head(m, e);
        var bank = new SpriteSheetExpression(id, Enumerable.Range(0, mouthFrames).Select(m => Frame(m, 1)), Enumerable.Range(0, mouthFrames).Select(m => Frame(m, 0)), profile.Blink);
        return new(samples == null ? [new CharacterPart("sprite", "sprite", Asset(0, 1), "root")] : CompanionParts.Define(), samples?.Anchors ?? [new AnchorDefinition("root")],
            expressions: [new ExpressionDefinition(id, Asset(0, 1), Asset(0, 0), name: profile.Name)],
            spriteSheet: new(samples == null ? "sprite" : "head", [bank], mouthFrames == 3 ? "legacy3" : "legacy8"),
            actions: samples?.Actions, attachments: samples?.Attachments,
            motion: new MotionProfile(blinkEnabled: profile.Blink, breathing: 1, sway: 0, headBounce: 0, headRotation: 0, bodyAnchor: "root", headAnchor: "root", viewportMotion: true));
    }
    public static string Asset(int mouth, int eye) => $"legacy/sprite-{mouth}-{eye}.png";
}
public sealed class LegacySpriteSheetSource : ICharacterDefinitionSource
{
    private readonly CharacterDefinition definition;
    public LegacySpriteSheetSource(int mouthFrames, ExpressionProfile? profile = null) => definition = LegacySpriteSheetAdapter.Convert(mouthFrames, profile);
    public CharacterDefinition Load() => definition;
}
