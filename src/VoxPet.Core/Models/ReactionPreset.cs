namespace VoxPet.Core.Models;

/// <summary>
/// 사용자가 선택할 수 있는 반응 조정 묶음. 음성/소음의 의미 분류는 수행하지 않는다.
/// </summary>
public enum ReactionPreset { Conversation, SoftVoice, Snappy }

/// <summary>
/// 관련 설정을 한 번에 교체해 Gate와 입력 범위가 서로 어긋나는 조정을 줄인다.
/// </summary>
public static class ReactionPresets
{
    // 인수 순서: Gate, 정규화 최소/최대(dBFS), 감도(배율), Attack/Release(ms).
    // 일반 대화는 기본 설정, 조용한 목소리는 입력 문턱을 낮추고 빠른 반응은 시정수를 줄인다.
    public static AudioSettings Create(ReactionPreset preset) => preset switch
    {
        ReactionPreset.Conversation => new(),
        ReactionPreset.SoftVoice => new(-65, -65, -20, 1.2, 50, 180),
        ReactionPreset.Snappy => new(-45, -45, -10, 1, 15, 80),
        _ => throw new ArgumentOutOfRangeException(nameof(preset))
    };
}
