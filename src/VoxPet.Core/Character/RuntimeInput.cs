using VoxPet.Core.Triggers;

namespace VoxPet.Core.Character;

public sealed record InputSnapshot
{
    public static InputSnapshot Empty { get; } = new();
    public IReadOnlyList<TriggerEvent> Events { get; }
    public InputSnapshot(IEnumerable<TriggerEvent>? events = null) => Events = DefinitionValidation.Copy(events ?? [], nameof(events));
}

public sealed record RuntimeInput
{
    public double Time { get; }
    public double DeltaTime { get; }
    public double VoiceLevel { get; }
    public bool? Blink { get; }
    public InputSnapshot Input { get; }
    public RuntimeInput(double time, double deltaTime, double voiceLevel, bool? blink = null, InputSnapshot? input = null)
    {
        Time = DefinitionValidation.Finite(time, nameof(time)); DeltaTime = DefinitionValidation.Finite(deltaTime, nameof(deltaTime));
        VoiceLevel = DefinitionValidation.Unit(voiceLevel, nameof(voiceLevel)); if (time < 0 || deltaTime < 0) throw new ArgumentOutOfRangeException(nameof(time));
        Blink = blink; Input = input ?? InputSnapshot.Empty;
    }
}
