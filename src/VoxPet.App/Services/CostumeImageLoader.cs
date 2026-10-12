using System.IO;
using System.Windows.Media.Imaging;

namespace VoxPet.App.Services;

internal static class CostumeImageLoader
{
    internal static (BitmapSource Image, int Width, int Height) Load(byte[] bytes)
    {
        if (bytes.Length is < 16 or > 16 * 1024 * 1024) throw new ArgumentException("이미지는16MB 이하입니다.");
        using var stream = new MemoryStream(bytes, false);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
        int width = decoder.Frames[0].PixelWidth, height = decoder.Frames[0].PixelHeight;
        if (width is < 1 or > 4096 || height is < 1 or > 4096 || (long)width * height > 8_000_000) throw new ArgumentException("이미지 크기를 확인하세요.");
        stream.Position = 0;
        var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream;
        if (width >= height) image.DecodePixelWidth = Math.Min(width, 1024); else image.DecodePixelHeight = Math.Min(height, 1024);
        image.EndInit(); image.Freeze(); return (image, width, height);
    }
}
