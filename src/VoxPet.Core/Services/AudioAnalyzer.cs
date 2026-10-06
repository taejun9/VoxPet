using System.Buffers.Binary;
using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

/// <summary>
/// little-endian 인터리브 PCM을 즉시 분석한다. 채널을 먼저 평균하지 않고 모든 샘플의
/// 제곱 에너지를 합산하므로 반대 위상 스테레오가 상쇄되어 무음으로 보이지 않는다.
/// 입력 크기에 비례하는 O(N) 처리만 수행하며 파일·UI·비동기 작업에 버퍼를 넘기지 않는다.
/// </summary>
public static class AudioAnalyzer
{
    /// <summary>
    /// 콜백이 소유한 버퍼를 읽어 RMS, 절댓값 Peak, RMS 기반 dBFS를 계산한다.
    /// 빈 버퍼는 무음이며 채널 프레임이 완성되지 않은 버퍼는 오류로 처리한다.
    /// </summary>
    public static AudioLevel Analyze(ReadOnlySpan<byte> buffer, PcmFormat format)
    {
        format.Validate();
        int width = format.BytesPerSample;
        // 한 프레임에는 채널 수만큼의 샘플이 있어야 한다. 잘린 프레임을 임의로 보완하지 않는다.
        if (buffer.Length % (width * format.Channels) != 0)
            throw new ArgumentException("불완전한 오디오 프레임입니다.", nameof(buffer));
        double sum = 0, peak = 0;
        int count = buffer.Length / width;
        for (int offset = 0; offset < buffer.Length; offset += width)
        {
            var bytes = buffer.Slice(offset, width);
            // 정수 PCM은 full scale로 나눈다. 8비트는 unsigned, 16/24/32비트는 signed다.
            // 24비트의 <<8 >>8은 최상위 부호 비트를 확장해 음수 샘플을 복원한다.
            double sample = format.Encoding == SampleEncoding.Float
                ? (width == 4 ? BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes))
                    : BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(bytes)))
                : width switch
                {
                    1 => (bytes[0] - 128) / 128.0,
                    2 => BinaryPrimitives.ReadInt16LittleEndian(bytes) / 32768.0,
                    3 => ((bytes[0] | bytes[1] << 8 | bytes[2] << 16) << 8 >> 8) / 8388608.0,
                    4 => BinaryPrimitives.ReadInt32LittleEndian(bytes) / 2147483648.0,
                    _ => throw new NotSupportedException()
                };
            // 손상된 float는 무음으로 대체한다. 극단값은 ±16으로 제한해 제곱합 overflow를 막고 일반 clipping은 보존한다.
            sample = double.IsFinite(sample) ? Math.Clamp(sample, -16, 16) : 0;
            sum += sample * sample;
            peak = Math.Max(peak, Math.Abs(sample));
        }
        // RMS = sqrt(sum(sample²)/N). 1e-6 바닥은 20*log10(1e-6) = -120 dBFS다.
        double rms = count == 0 ? 0 : Math.Sqrt(sum / count);
        return new(rms, peak, 20 * Math.Log10(Math.Max(rms, 1e-6)));
    }
}
