using VoxPet.Core.Character;

namespace VoxPet.Core.Triggers;

/// <summary>설정한 입력만 일시 보관하며 voice edge/interval 지연을 actionID 요청으로 변환한다.</summary>
public sealed class TriggerEngine
{
    private readonly IReadOnlyList<TriggerDefinition> definitions;
    private readonly double[] next;
    private readonly bool[] above;
    private readonly HashSet<string> held = new(StringComparer.Ordinal);
    private double last;
    public IReadOnlyList<TriggerRequest> Requests { get; private set; } = Array.Empty<TriggerRequest>();
    public TriggerEngine(IEnumerable<TriggerDefinition> definitions, double startedAt = 0)
    {
        this.definitions = DefinitionValidation.Copy(definitions, nameof(definitions));
        if (this.definitions.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != this.definitions.Count) throw new ArgumentException("트리거ID 중복입니다.");
        next = new double[this.definitions.Count]; above = new bool[this.definitions.Count]; Reset(startedAt);
    }
    private void Time(double seconds)
    {
        DefinitionValidation.Finite(seconds, nameof(seconds));
        if (seconds < last || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        last = seconds;
    }
    public void Reset(double seconds)
    {
        Time(seconds); held.Clear(); Array.Clear(above);
        Requests = Array.Empty<TriggerRequest>();
        for (int i = 0; i < definitions.Count; i++) next[i] = definitions[i].StartImmediately ? seconds : seconds + definitions[i].IntervalSeconds;
    }
    public IReadOnlyList<string> Update(double seconds, double voiceLevel, IEnumerable<TriggerEvent>? events = null)
    {
        Time(seconds); voiceLevel = double.IsFinite(voiceLevel) ? Math.Clamp(voiceLevel, 0, 1) : 0;
        var requested = new List<string>(); var details = new List<TriggerRequest>(); var unique = new HashSet<string>(StringComparer.Ordinal);
        void Request(TriggerDefinition definition)
        {
            if (unique.Add(definition.ActionId))
            {
                requested.Add(definition.ActionId); details.Add(new(definition.ActionId, definition.Id, definition.Kind));
            }
        }
        foreach (var signal in events ?? [])
        {
            ArgumentNullException.ThrowIfNull(signal);
            if (signal.Kind == TriggerKind.Hotkey)
            {
                if (!signal.Pressed) { held.Remove(signal.Key!); continue; }
                if (!definitions.Any(d => d.Enabled && d.Kind == TriggerKind.Hotkey && d.Key == signal.Key && d.Modifiers == signal.Modifiers)) continue;
                if (!signal.IsPulse && !held.Add(signal.Key!)) continue;
            }
            if (!signal.Pressed) continue;
            foreach (var definition in definitions.Where(d => d.Enabled && d.Kind == signal.Kind))
            {
                bool matches = signal.Kind switch
                {
                    TriggerKind.Hotkey => definition.Key == signal.Key && definition.Modifiers == signal.Modifiers,
                    TriggerKind.Mouse => definition.Button == signal.Button,
                    TriggerKind.Manual => definition.Key == signal.Key,
                    _ => false
                };
                if (matches) Request(definition);
            }
        }
        for (int i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i]; if (!definition.Enabled) continue;
            if (definition.Kind == TriggerKind.Interval && seconds >= next[i])
            { Request(definition); next[i] = seconds + definition.IntervalSeconds; }
            if (definition.Kind == TriggerKind.VoiceThreshold)
            {
                bool now = voiceLevel >= definition.Threshold;
                if (now && !above[i]) Request(definition);
                above[i] = now;
            }
        }
        Requests = Array.AsReadOnly(details.ToArray());
        return Array.AsReadOnly(requested.ToArray());
    }
}
