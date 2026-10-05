using System.Buffers.Binary;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Models;

namespace VoxPet.App.Services;

public static class CharacterSheetLoader
{
    // Called on a worker thread. OnLoad and Freeze release the source file and allow UI transfer.
    public static ImageSource[,] Load(string path)
    {
        using var file = File.OpenRead(path);
        if (file.Length is < 33 or > 16 * 1024 * 1024)
            throw new ArgumentException("PNG 파일은 16MB 이하여야 합니다.");
        var data = new byte[(int)file.Length];
        file.ReadExactly(data);
        ReadOnlySpan<byte> header = data;
        if (!header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
            BinaryPrimitives.ReadUInt32BigEndian(header[8..12]) != 13 ||
            !header[12..16].SequenceEqual("IHDR"u8) || header[24] != 8 || header[25] != 6)
            throw new ArgumentException("8비트 RGBA 투명 PNG 시트를 선택하세요.");
        int width = checked((int)BinaryPrimitives.ReadUInt32BigEndian(header[16..20]));
        int height = checked((int)BinaryPrimitives.ReadUInt32BigEndian(header[20..24]));
        int cell = SpriteSheetLayout.Validate(width, height);
        using var stream = new MemoryStream(data, writable: false);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        if (frame.PixelWidth != width || frame.PixelHeight != height)
            throw new ArgumentException("PNG 크기를 확인하세요.");
        var sprites = new ImageSource[3, 2];
        for (int mouth = 0; mouth < 3; mouth++)
            for (int eye = 0; eye < 2; eye++)
            {
                var cropped = new CroppedBitmap(frame, new Int32Rect(mouth * cell, (eye == 1 ? 0 : 1) * cell, cell, cell));
                sprites[mouth, eye] = Align(cropped, cell);
            }
        return sprites;
    }
    private static BitmapSource Align(BitmapSource source, int cell)
    {
        var bgra = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int stride = cell * 4;
        var pixels = new byte[stride * cell]; bgra.CopyPixels(pixels, stride, 0);
        int left = cell, top = cell, right = -1, bottom = -1;
        bool transparent = false;
        for (int y = 0; y < cell; y++)
            for (int x = 0; x < cell; x++)
            {
                byte alpha = pixels[y * stride + x * 4 + 3];
                transparent |= alpha == 0;
                if (alpha <= 32) continue;
                left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y);
            }
        if (right < left || !transparent)
            throw new ArgumentException("각 셀에는 투명 여백과 캐릭터가 필요합니다.");
        // Keep two pixels of antialiasing and align the feet/center across all mouth and blink states.
        left = Math.Max(0, left - 2); top = Math.Max(0, top - 2);
        right = Math.Min(cell - 1, right + 2); bottom = Math.Min(cell - 1, bottom + 2);
        int width = right - left + 1, height = bottom - top + 1;
        int targetX = (cell - width) / 2;
        int targetY = Math.Max(0, cell - Math.Max(1, cell / 32) - height);
        var aligned = new byte[pixels.Length];
        for (int y = 0; y < height; y++)
            Buffer.BlockCopy(pixels, (top + y) * stride + left * 4, aligned, (targetY + y) * stride + targetX * 4, width * 4);
        var bitmap = BitmapSource.Create(cell, cell, 96, 96, PixelFormats.Bgra32, null, aligned, stride);
        bitmap.Freeze(); return bitmap;
    }
}
