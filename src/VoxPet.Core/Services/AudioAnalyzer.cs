using System.Buffers.Binary;
using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

/// <summary>Bounded analysis of little-endian interleaved PCM, without channel cancellation.</summary>
public static class AudioAnalyzer
{
    public static AudioLevel Analyze(ReadOnlySpan<byte> buffer, PcmFormat format)
    {
        format.Validate();
        int width = format.BytesPerSample;
        if (buffer.Length % (width * format.Channels) != 0)
            throw new ArgumentException("불완전한 오디오 프레임입니다.", nameof(buffer));
        double sum = 0, peak = 0;
        int count = buffer.Length / width;
        for (int offset = 0; offset < buffer.Length; offset += width)
        {
            var bytes = buffer.Slice(offset, width);
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
            // Corrupt float samples become silence; avoid overflow while retaining clipping diagnostics.
            sample = double.IsFinite(sample) ? Math.Clamp(sample, -16, 16) : 0;
            sum += sample * sample;
            peak = Math.Max(peak, Math.Abs(sample));
        }
        double rms = count == 0 ? 0 : Math.Sqrt(sum / count);
        return new(rms, peak, 20 * Math.Log10(Math.Max(rms, 1e-6)));
    }
}
