using VoxPet.Core.Character;

namespace VoxPet.Core.Attachments;

/// <summary>착용 중 독립적으로 반복하는 PNG 시퀀스. 프레임 선택은 파일 I/O나 가변 상태를 사용하지 않는다.</summary>
public sealed record AttachmentAnimation
{
    public const int MaxFrames = 64;
    public IReadOnlyList<string> Frames { get; }
    public double FrameDurationMs { get; }
    public AttachmentAnimation(IEnumerable<string> frames, double frameDurationMs = 100)
    {
        ArgumentNullException.ThrowIfNull(frames);
        var copy = frames.Take(MaxFrames + 1).ToArray();
        if (copy.Length is < 2 or > MaxFrames) throw new ArgumentException("애니메이션은2~64프레임이어야 합니다.", nameof(frames));
        foreach (string path in copy) DefinitionValidation.Asset(path, ".png", nameof(frames));
        DefinitionValidation.Finite(frameDurationMs, nameof(frameDurationMs));
        if (frameDurationMs is < 16 or > 10000) throw new ArgumentOutOfRangeException(nameof(frameDurationMs), "프레임 간격은16~10000ms입니다.");
        Frames = Array.AsReadOnly(copy); FrameDurationMs = frameDurationMs;
    }
    public string FrameAt(double time)
    {
        DefinitionValidation.Finite(time, nameof(time));
        if (time < 0) throw new ArgumentOutOfRangeException(nameof(time));
        double seconds = FrameDurationMs / 1000;
        // 나누기 전에 modulo를 적용해 매우 큰 유한 시간에서도 overflow하지 않는다.
        int index = Math.Min(Frames.Count - 1, (int)((time % (seconds * Frames.Count)) / seconds));
        return Frames[index];
    }
}
