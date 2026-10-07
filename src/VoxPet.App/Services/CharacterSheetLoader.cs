using System.Buffers.Binary;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Models;

namespace VoxPet.App.Services;

/// <summary>
/// 로컬 사용자 PNG를 검증해 두 창이 공유할 frozen 상태 이미지 6개로 변환한다.
/// </summary>
public static class CharacterSheetLoader
{
    /// <summary>
    /// 작업 스레드에서 호출한다. 파일 크기·헤더·셀 크기를 먼저 검사하고 전체 시트를 완성해 반환한다.
    /// OnLoad는 스트림 의존을 끊고 Freeze는 결과를 UI 스레드에서 사용할 수 있게 한다.
    /// 반환 인덱스는 [입 상태, 눈 상태]이며 눈 0=감음, 1=뜸이다.
    /// </summary>
    public static ImageSource[,] Load(string path)
    {
        using var file = File.OpenRead(path);
        if (file.Length is < 33 or > 16 * 1024 * 1024)
            throw new ArgumentException("PNG 파일은 16MB 이하여야 합니다.");
        var data = new byte[(int)file.Length]; file.ReadExactly(data);
        return Load(data);
    }
    /// <summary>관리 저장소의 제한된 바이트 복사본에도 동일한 PNG 검증을 적용한다.</summary>
    public static ImageSource[,] Load(byte[] data)
    {
        if (data.Length is < 33 or > 16 * 1024 * 1024)
            throw new ArgumentException("PNG 파일은 16MB 이하여야 합니다.");
        // PNG signature와 첫 IHDR의 길이·색 형식(6=RGBA)·비트 깊이를 디코딩 전에 검사한다.
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
        // 파일 첫 행은 눈 뜸이지만 내부 eye 인덱스는 1이다. 행 순서를 뒤집어 기존 렌더러 계약에 맞춘다.
        var sprites = new ImageSource[3, 2];
        for (int mouth = 0; mouth < 3; mouth++)
            for (int eye = 0; eye < 2; eye++)
            {
                var cropped = new CroppedBitmap(frame, new Int32Rect(mouth * cell, (eye == 1 ? 0 : 1) * cell, cell, cell));
                sprites[mouth, eye] = Align(cropped, cell);
            }
        return sprites;
    }
    /// <summary>
    /// 알파 영역의 중앙과 바닥을 같은 위치에 맞춘다. 상태마다 몸 크기를 바꾸는 리사이즈는 수행하지 않는다.
    /// </summary>
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
                // 실제 투명 픽셀 존재는 alpha=0으로 확인하고, 영역 계산은 alpha>32 픽셀을 사용한다.
                byte alpha = pixels[y * stride + x * 4 + 3];
                transparent |= alpha == 0;
                if (alpha <= 32) continue;
                left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y);
            }
        if (right < left || !transparent)
            throw new ArgumentException("각 셀에는 투명 여백과 캐릭터가 필요합니다.");
        // 경계의 antialiasing을 위해 2px 여유를 남긴다. 발밑 여백은 셀 한 변의 약 1/32로 통일한다.
        left = Math.Max(0, left - 2); top = Math.Max(0, top - 2);
        right = Math.Min(cell - 1, right + 2); bottom = Math.Min(cell - 1, bottom + 2);
        int width = right - left + 1, height = bottom - top + 1;
        int targetX = (cell - width) / 2;
        int targetY = Math.Max(0, cell - Math.Max(1, cell / 32) - height);
        var aligned = new byte[pixels.Length];
        for (int y = 0; y < height; y++)
            Buffer.BlockCopy(pixels, (top + y) * stride + left * 4, aligned, (targetY + y) * stride + targetX * 4, width * 4);
        var bitmap = BitmapSource.Create(cell, cell, 96, 96, PixelFormats.Bgra32, null, aligned, stride);
        // 이 셀을 Freeze해 UI 전달을 허용한다. 모든 셀의 처리가 성공한 뒤에만 Load가 배열을 반환한다.
        bitmap.Freeze(); return bitmap;
    }
}
