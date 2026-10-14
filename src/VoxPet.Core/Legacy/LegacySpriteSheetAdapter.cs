using VoxPet.Core.Character;
using VoxPet.Core.Motion;
using CharacterDefinition = VoxPet.Core.Character.CharacterDefinition;

namespace VoxPet.Core.Legacy;

/// <summary>full-canvas 시트는 Core 조합 정책이 opaque frame asset을 선택한다.</summary>
public static class LegacySpriteSheetAdapter
{
    public static CharacterDefinition Convert(int mouthFrames)
    {
        if (mouthFrames is not (3 or 8)) throw new ArgumentOutOfRangeException(nameof(mouthFrames));
        return new([new CharacterPart("sprite", "sprite", Asset(0, 1), "root")], [new AnchorDefinition("root")],
            motion: new MotionProfile(blinkEnabled: false, idleEnabled: false, mouth: 0, bodyBounce: 0, headBounce: 0, headRotation: 0, bodyAnchor: "root", headAnchor: "root"));
    }
    public static string Asset(int mouth, int eye) => $"legacy/sprite-{mouth}-{eye}.png";
}
public sealed class LegacySpriteSheetSource : ICharacterDefinitionSource
{
    private readonly CharacterDefinition definition;
    public LegacySpriteSheetSource(int mouthFrames) => definition = LegacySpriteSheetAdapter.Convert(mouthFrames);
    public CharacterDefinition Load() => definition;
}
