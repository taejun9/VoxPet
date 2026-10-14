using VoxPet.Core.Character;

namespace VoxPet.Core.Triggers;

/// <summary>현재 tick의 요청 출처. 자동 예약과 명시 입력의 조정을 위해 제공하며 기록하지 않는다.</summary>
public sealed record TriggerRequest
{
    public string ActionId { get; }
    public string TriggerId { get; }
    public TriggerKind Kind { get; }
    public TriggerRequest(string actionId, string triggerId, TriggerKind kind)
    {
        ActionId = DefinitionValidation.Id(actionId, nameof(actionId)); TriggerId = DefinitionValidation.Id(triggerId, nameof(triggerId));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind;
    }
}
