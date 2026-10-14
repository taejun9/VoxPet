namespace VoxPet.Core.Character;

/// <summary>Runtime 선택 상태의 불변 snapshot. 모델만 추가하며 기존 상태 VM은 유지한다.</summary>
public sealed record CharacterState
{
    public double VoiceLevel { get; }
    public string ExpressionId { get; }
    public string? ActionId { get; }
    public IReadOnlyList<string> AttachmentIds { get; }

    public CharacterState(double voiceLevel = 0, string expressionId = "neutral", string? actionId = null,
        IEnumerable<string>? attachmentIds = null)
    {
        VoiceLevel = DefinitionValidation.Unit(voiceLevel, nameof(voiceLevel));
        ExpressionId = DefinitionValidation.Id(expressionId, nameof(expressionId));
        ActionId = actionId is null ? null : DefinitionValidation.Id(actionId, nameof(actionId));
        AttachmentIds = DefinitionValidation.Copy(attachmentIds ?? [], nameof(attachmentIds));
        foreach (string id in AttachmentIds) DefinitionValidation.Id(id, nameof(attachmentIds));
        if (AttachmentIds.Distinct(StringComparer.Ordinal).Count() != AttachmentIds.Count)
            throw new ArgumentException("부착물ID가 중복됩니다.", nameof(attachmentIds));
    }
}
