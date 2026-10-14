namespace VoxPet.Core.Actions;

public enum ActionPlaybackMode { Once, Loop }
public sealed record ActionPlayback(ActionPlaybackMode Mode = ActionPlaybackMode.Once, bool Interruptible = true, bool ReturnToIdle = true)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Mode)) throw new ArgumentOutOfRangeException(nameof(Mode));
    }
}
