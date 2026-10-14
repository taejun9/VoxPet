using VoxPet.Core.Character;
using VoxPet.Core.Triggers;

namespace VoxPet.Core.Actions;

public sealed record ActionBinding
{
    public string ActionId { get; }
    public bool Enabled { get; }
    public Transform2D Transform { get; }
    public IReadOnlyList<TriggerDefinition> Triggers { get; }
    public ActionBinding(string actionId, IEnumerable<TriggerDefinition> triggers, bool enabled = true, Transform2D? transform = null)
    {
        ActionId = DefinitionValidation.Id(actionId, nameof(actionId)); Enabled = enabled;
        Transform = transform ?? Transform2D.Identity;
        Triggers = DefinitionValidation.Copy(triggers, nameof(triggers));
        if (Triggers.Any(t => t.ActionId != actionId) || Triggers.Select(t => t.Id).Distinct(StringComparer.Ordinal).Count() != Triggers.Count) throw new ArgumentException("동작 트리거 참조를 확인하세요.");
    }
}

public sealed record ActionSettings
{
    public IReadOnlyList<ActionBinding> Bindings { get; }
    public ActionSettings(IEnumerable<ActionBinding> bindings)
    {
        Bindings = DefinitionValidation.Copy(bindings, nameof(bindings));
        if (Bindings.Select(b => b.ActionId).Distinct(StringComparer.Ordinal).Count() != Bindings.Count ||
            Bindings.SelectMany(b => b.Triggers).Select(t => t.Id).Distinct(StringComparer.Ordinal).Count() != Bindings.Sum(b => b.Triggers.Count)) throw new ArgumentException("동작/트리거ID 중복입니다.");
    }
    public IEnumerable<TriggerDefinition> EnabledTriggers => Bindings.Where(b => b.Enabled).SelectMany(b => b.Triggers).Where(t => t.Enabled);
}
