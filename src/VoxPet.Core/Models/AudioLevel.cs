namespace VoxPet.Core.Models;

/// <summary>
/// 콜백 밖으로 전달하는 불변 측정값. PCM 버퍼 참조는 보관하지 않는다.
/// Rms와 Peak는 full scale 대비 비율, Dbfs는 RMS로부터 계산한 상대 dB 값이다.
/// clipping 진단을 위해 Rms/Peak는 1을 초과할 수 있으며 애니메이션 측에서 0~1로 제한한다.
/// </summary>
public sealed record AudioLevel(double Rms, double Peak, double Dbfs)
{
    // 무음의 log10(0)을 피하기 위해 dBFS 표시 바닥을 -120으로 정의한다.
    public static AudioLevel Silence { get; } = new(0, 0, -120);
}

/// <summary>
/// Windows 입력의 실제 인코딩을 표현한다. Float를 기본 형식으로 가정하지 않는다.
/// </summary>
public enum SampleEncoding { Pcm, Float }

/// <summary>
/// 인터리브 PCM 형식. Channels는 프레임당 샘플 수이며 BitsPerSample은 채널 하나의 비트 수다.
/// </summary>
public sealed record PcmFormat(SampleEncoding Encoding, int BitsPerSample, int Channels)
{
    public int BytesPerSample => BitsPerSample / 8;
    /// <summary>
    /// 분석기가 디코딩할 수 있는 형식만 허용해 샘플 폭 0이나 잘못된 채널 수를 사전에 막는다.
    /// </summary>
    public void Validate()
    {
        if (Channels is < 1 or > 32 ||
            (Encoding == SampleEncoding.Pcm && BitsPerSample is not (8 or 16 or 24 or 32)) ||
            (Encoding == SampleEncoding.Float && BitsPerSample is not (32 or 64)) ||
            !Enum.IsDefined(Encoding))
            throw new NotSupportedException("지원하지 않는 마이크 샘플 형식입니다.");
    }
}
