namespace VoxPet.Core.Models;

/// <summary>Only measurements survive a callback; PCM is never retained.</summary>
public sealed record AudioLevel(double Rms, double Peak, double Dbfs)
{
    public static AudioLevel Silence { get; } = new(0, 0, -120);
}

public enum SampleEncoding { Pcm, Float }

public sealed record PcmFormat(SampleEncoding Encoding, int BitsPerSample, int Channels)
{
    public int BytesPerSample => BitsPerSample / 8;
    public void Validate()
    {
        if (Channels is < 1 or > 32 ||
            (Encoding == SampleEncoding.Pcm && BitsPerSample is not (8 or 16 or 24 or 32)) ||
            (Encoding == SampleEncoding.Float && BitsPerSample is not (32 or 64)) ||
            !Enum.IsDefined(Encoding))
            throw new NotSupportedException("지원하지 않는 마이크 샘플 형식입니다.");
    }
}
