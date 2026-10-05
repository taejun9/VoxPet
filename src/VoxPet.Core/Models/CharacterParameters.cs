namespace VoxPet.Core.Models;

public enum MouthState { Closed, Half, Open }
public sealed record CharacterParameters(double MouthOpen, double BodyBounce, double HeadTilt,
    double EarMotion, double EyeOpen, MouthState Mouth);
