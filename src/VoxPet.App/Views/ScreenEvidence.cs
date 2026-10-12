using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VoxPet.App.Views;

/// <summary>일회용 Windows CI에서 표시된 앱 창의 실제 desktop pixels만 캡처. 일반 앱에서는 호출하지 않는다.</summary>
internal static class ScreenEvidence
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public uint Size; public int Width, Height; public ushort Planes, BitCount;
        public uint Compression, ImageSize; public int XPels, YPels; public uint ColorsUsed, ColorsImportant;
    }
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, out Rect rect);
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr window, IntPtr dc);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateDIBSection(IntPtr dc, ref BitmapInfo info, uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint operation);
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr value);
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(IntPtr dc);
    private static readonly List<object> records = [];
    private static string? Folder => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ? Environment.GetEnvironmentVariable("RUNNER_TEMP") : null;
    public static async Task CaptureAsync(Window window, string name, Action<bool, string> check, object? scenario = null)
    {
        if (Folder is not { } folder) return;
        if (name.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')) throw new ArgumentException("Screen evidence name");
        bool prior = window.Topmost;
        try
        {
            if (!window.IsVisible || window.WindowState == WindowState.Minimized) throw new InvalidOperationException("Screen target is not visible");
            window.SetCurrentValue(Window.TopmostProperty, true); window.Activate(); window.UpdateLayout();
            await Task.Delay(100);
            var handle = new WindowInteropHelper(window).Handle;
            if (!GetWindowRect(handle, out var bounds)) throw new InvalidOperationException("Screen bounds unavailable");
            int width = bounds.Right - bounds.Left, height = bounds.Bottom - bounds.Top;
            var desktop = System.Windows.Forms.SystemInformation.VirtualScreen;
            if (window.WindowState == WindowState.Maximized)
            {
                var visible = System.Drawing.Rectangle.Intersect(desktop, new(bounds.Left, bounds.Top, width, height));
                bounds.Left = visible.Left; bounds.Top = visible.Top; bounds.Right = visible.Right; bounds.Bottom = visible.Bottom;
                width = visible.Width; height = visible.Height;
            }
            if (width <= 0 || height <= 0 || width > 3000 || height > 2000 || !desktop.Contains(new System.Drawing.Rectangle(bounds.Left, bounds.Top, width, height)))
                throw new InvalidOperationException("Screen target is outside desktop");
            var screen = GetDC(IntPtr.Zero); var memory = CreateCompatibleDC(screen); IntPtr bitmap = IntPtr.Zero, priorBitmap = IntPtr.Zero;
            byte[] pixels = new byte[width * height * 4];
            try
            {
                var info = new BitmapInfo { Size = (uint)Marshal.SizeOf<BitmapInfo>(), Width = width, Height = -height, Planes = 1, BitCount = 32 };
                bitmap = CreateDIBSection(screen, ref info, 0, out var bits, IntPtr.Zero, 0);
                if (screen == IntPtr.Zero || memory == IntPtr.Zero || bitmap == IntPtr.Zero || bits == IntPtr.Zero) throw new InvalidOperationException("Screen buffer unavailable");
                priorBitmap = SelectObject(memory, bitmap);
                // SRCCOPY|CAPTUREBLT는 표시된 layered 방송창도 포함한다. PrintWindow/RenderTargetBitmap 대체 경로 없음.
                if (!BitBlt(memory, 0, 0, width, height, screen, bounds.Left, bounds.Top, 0x40CC0020)) throw new InvalidOperationException("Desktop pixel copy failed");
                Marshal.Copy(bits, pixels, 0, pixels.Length);
            }
            finally
            {
                if (priorBitmap != IntPtr.Zero) SelectObject(memory, priorBitmap);
                if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
                if (memory != IntPtr.Zero) DeleteDC(memory);
                if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
            }
            var colors = new HashSet<int>();
            for (int i = 0; i < pixels.Length; i += 4) { pixels[i + 3] = 255; if (i % 64 == 0) colors.Add((pixels[i] << 16) | (pixels[i + 1] << 8) | pixels[i + 2]); }
            bool valid = colors.Count > 24 && GetForegroundWindow() == handle;
            check(valid, "screen_" + name);
            var image = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4); image.Freeze();
            string fileName = "voxpet-screen-" + name + ".png";
            await Task.Run(() => { var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); using var file = File.Create(Path.Combine(folder, fileName)); encoder.Save(file); });
            var record = new { fileName, capture = "desktop-bitblt-captureblt", actualDesktopPixels = true, syntheticRender = false, privateAssets = false, os = Environment.OSVersion.VersionString, utc = DateTime.UtcNow, hwnd = handle.ToInt64(), window.Title, bounds.Left, bounds.Top, width, height, uniqueColors = colors.Count, foreground = GetForegroundWindow().ToInt64(), valid, pixelSha256 = Convert.ToHexString(SHA256.HashData(pixels)), scenario };
            records.Add(record);
            File.WriteAllText(Path.Combine(folder, "voxpet-screen-" + name + ".json"), JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(Path.Combine(folder, "voxpet-screen-index.json"), JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { window.SetCurrentValue(Window.TopmostProperty, prior); }
    }
}
