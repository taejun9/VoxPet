using VoxPet.Core.Character;

namespace VoxPet.Core.Motion;

public enum CompanionActing { None, Neulbo, Pelly }

public sealed record MotionProfile
{
    public static MotionProfile Default { get; } = new();
    public static MotionProfile LegacyImageSet { get; } = new(headBounce: 0, headRotation: 0, breathing: 1, sway: 0, bodyAnchor: "root", viewportMotion: true);
    public static MotionProfile Neulbo { get; } = new(acting: CompanionActing.Neulbo, breathing: .35, sway: .15, bodyBounce: 4.5, headBounce: 1.8, headRotation: 1.2, intervalMin: 2.5, intervalMax: 6.5, blinkDuration: .15);
    public static MotionProfile Pelly { get; } = new(acting: CompanionActing.Pelly, breathing: .45, sway: .22, bodyBounce: 6.0, headBounce: 2.5, headRotation: 2.0, intervalMin: 1.8, intervalMax: 4.5, blinkDuration: .12);
    public static MotionProfile ForCharacter(string? characterId) => characterId switch { "pelly" => Pelly, "neulbo" => Neulbo, _ => Default };
    public bool BlinkEnabled { get; }
    public double IntervalMin { get; }
    public double IntervalMax { get; }
    public double BlinkDuration { get; }
    public bool IdleEnabled { get; }
    public double Breathing { get; }
    public double Sway { get; }
    public double Mouth { get; }
    public double BodyBounce { get; }
    public double HeadBounce { get; }
    public double HeadRotation { get; }
    public double MouthTravel { get; }
    public string BodyAnchor { get; }
    public string HeadAnchor { get; }
    public bool ViewportMotion { get; }
    public CompanionActing Acting { get; }
    public MotionProfile(bool blinkEnabled = true, double intervalMin = 2, double intervalMax = 6, double blinkDuration = .14,
        bool idleEnabled = true, double breathing = .4, double sway = .2, double mouth = 1, double bodyBounce = 5,
        double headBounce = 2, double headRotation = 1.5, double mouthTravel = 0, string bodyAnchor = "body", string headAnchor = "head", bool viewportMotion = false, CompanionActing acting = CompanionActing.None)
    {
        if (!Enum.IsDefined(acting)) throw new ArgumentOutOfRangeException(nameof(acting));
        Acting = acting;
        var voice = new VoiceMotion(mouth, bodyBounce, headBounce, headRotation); var idle = new IdleMotion(breathing, sway);
        IntervalMin = VoiceMotion.Amount(intervalMin, 3600, nameof(intervalMin)); IntervalMax = VoiceMotion.Amount(intervalMax, 3600, nameof(intervalMax));
        BlinkDuration = VoiceMotion.Amount(blinkDuration, 10, nameof(blinkDuration));
        if (intervalMin <= 0 || intervalMax < intervalMin) throw new ArgumentOutOfRangeException(nameof(intervalMin));
        BlinkEnabled = blinkEnabled; IdleEnabled = idleEnabled; Breathing = idle.Breathing; Sway = idle.Sway;
        Mouth = voice.Mouth; BodyBounce = voice.BodyBounce; HeadBounce = voice.HeadBounce; HeadRotation = voice.HeadRotation;
        MouthTravel = VoiceMotion.Amount(mouthTravel, 512, nameof(mouthTravel));
        BodyAnchor = DefinitionValidation.Id(bodyAnchor, nameof(bodyAnchor)); HeadAnchor = DefinitionValidation.Id(headAnchor, nameof(headAnchor)); ViewportMotion = viewportMotion;
    }
    public MotionEngine CreateEngine(Random? random = null)
    {
        var sources = new List<IMotionSource> { Acting == CompanionActing.None ? new VoiceMotion(Mouth, BodyBounce, HeadBounce, HeadRotation) : new VoiceMotion(Mouth, 0, 0, 0) };
        if (IdleEnabled && Acting == CompanionActing.None) sources.Add(new IdleMotion(Breathing, Sway));
        if (BlinkEnabled) sources.Add(new BlinkMotion(random, IntervalMin, IntervalMax, BlinkDuration));
        return new(sources);
    }
}
