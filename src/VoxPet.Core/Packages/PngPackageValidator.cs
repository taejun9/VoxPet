using System.Buffers.Binary;
using System.IO.Compression;
using VoxPet.Core.Rendering;

namespace VoxPet.Core.Packages;

internal static class PngPackageValidator
{
    public static AssetDimensions Validate(ReadOnlySpan<byte> bytes, VoxPetPackageLimits limits)
    {
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length < 45 || !bytes[..8].SequenceEqual(signature)) throw new VoxPetPackageException("PNG signature가 잘못되었습니다.");
        int position = 8, width = 0, height = 0; bool header = false, data = false, end = false, palette = false; int color = 0, interlace = 0;
        using var compressed = new MemoryStream();
        while (position < bytes.Length)
        {
            if (bytes.Length - position < 12) throw new VoxPetPackageException("PNG chunk가 잘렸습니다.");
            uint length = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(position, 4));
            if (length > (uint)(bytes.Length - position - 12)) throw new VoxPetPackageException("PNG chunk 크기가 잘못되었습니다.");
            int size = (int)length; var type = bytes.Slice(position + 4, 4); var payload = bytes.Slice(position + 8, size);
            uint stored = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(position + 8 + size, 4));
            if (Crc(bytes.Slice(position + 4, size + 4)) != stored) throw new VoxPetPackageException("PNG CRC가 잘못되었습니다.");
            if (!header)
            {
                if (!type.SequenceEqual("IHDR"u8) || size != 13) throw new VoxPetPackageException("PNG IHDR이 필요합니다.");
                uint w = BinaryPrimitives.ReadUInt32BigEndian(payload), h = BinaryPrimitives.ReadUInt32BigEndian(payload[4..]);
                if (w == 0 || h == 0 || w > limits.PngDimension || h > limits.PngDimension || (long)w * h > limits.PngPixels ||
                    payload[8] != 8 || payload[9] is not (0 or 2 or 3 or 4 or 6) || payload[10] != 0 || payload[11] != 0 || payload[12] > 1)
                    throw new VoxPetPackageException("PNG 크기/형식을 확인하세요.");
                width = (int)w; height = (int)h; color = payload[9]; interlace = payload[12]; header = true;
            }
            else if (type.SequenceEqual("IHDR"u8)) throw new VoxPetPackageException("PNG IHDR 중복입니다.");
            if (type.SequenceEqual("acTL"u8)) throw new VoxPetPackageException("animated PNG는 지원하지 않습니다.");
            if (type.SequenceEqual("PLTE"u8)) { if (size is < 3 or > 768 || size % 3 != 0 || data) throw new VoxPetPackageException("PNG palette 오류입니다."); palette = true; }
            if (type.SequenceEqual("IDAT"u8)) { data = true; compressed.Write(payload); }
            if (type.SequenceEqual("IEND"u8))
            { if (size != 0 || position + 12 != bytes.Length) throw new VoxPetPackageException("PNG IEND를 확인하세요."); end = true; }
            position += size + 12;
        }
        if (!data || !end || color == 3 && !palette) throw new VoxPetPackageException("PNG data/end가 없습니다.");
        compressed.Position = 0; using var decoded = new ZLibStream(compressed, CompressionMode.Decompress);
        int channels = color switch { 0 or 3 => 1, 2 => 3, 4 => 2, _ => 4 };
        int[] startX = [0, 4, 0, 2, 0, 1, 0], startY = [0, 0, 4, 0, 2, 0, 1], stepX = [8, 8, 4, 4, 2, 2, 1], stepY = [8, 8, 8, 4, 4, 2, 2];
        for (int pass = 0; pass < (interlace == 0 ? 1 : 7); pass++)
        {
            int w = interlace == 0 ? width : width <= startX[pass] ? 0 : (width - startX[pass] + stepX[pass] - 1) / stepX[pass];
            int h = interlace == 0 ? height : height <= startY[pass] ? 0 : (height - startY[pass] + stepY[pass] - 1) / stepY[pass];
            if (w == 0 || h == 0) continue; byte[] row = new byte[checked(w * channels + 1)];
            for (int y = 0; y < h; y++) { decoded.ReadExactly(row); if (row[0] > 4) throw new VoxPetPackageException("PNG filter 오류입니다."); }
        }
        if (decoded.ReadByte() != -1) throw new VoxPetPackageException("PNG 실제 이미지 data 크기가 다릅니다.");
        return new(width, height);
    }
    internal static uint Crc(ReadOnlySpan<byte> bytes)
    {
        uint crc = uint.MaxValue;
        foreach (byte value in bytes)
        {
            crc ^= value;
            for (int i = 0; i < 8; i++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0);
        }
        return ~crc;
    }
}
