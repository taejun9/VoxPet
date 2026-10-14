using VoxPet.App.ViewModels;
using VoxPet.Core.Services;

namespace VoxPet.App.Services;

/// <summary>메모리의 캐릭터 실행 조정. MainViewModel은 음량과 monotonic time만 전달한다.</summary>
public sealed class CharacterRuntimeCoordinator(CharacterViewModel character, HandMotionViewModel hands, ImageRuntimeViewModel imageSets, NativeCharacterViewModel packages)
{
    private readonly CharacterAnimator animator = new();
    public void UpdateCharacter(double voice, double now) => character.Update(animator.Update(voice, now), now);
    public void Update(double voice, double now)
    {
        hands.Update(now); UpdateCharacter(voice, now);
        if (packages.IsActive) packages.Update(now, voice);
        else imageSets.Update(now, voice, character.IsBlinking, character.ExpressionKind);
    }
}
