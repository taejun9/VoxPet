namespace VoxPet.Core.Actions;

public static class ActionPriority
{
    public const int Idle = 0, VoiceMotion = 10, Expression = 20, AutomaticAction = 30, UserGesture = 50, Weapon = 80, System = 100;
}
