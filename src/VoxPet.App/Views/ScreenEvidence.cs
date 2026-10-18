using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Rendering;

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
    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();
    internal sealed record PreviewExpectation(FrameworkElement Region, MotionSurface Surface, RenderFrame Frame);
    private sealed record PreviewReference(int Left, int Top, int Width, int Height, byte[] Pixels, BitmapSource Image, bool FrameCurrent);
    private sealed record PreviewMatch(bool Matches, double ChangedFraction, double MaximumTileFraction);
    private static PreviewReference Reference(PreviewExpectation expected)
    {
        var element = expected.Region; var dpi = VisualTreeHelper.GetDpi(element);
        var start = element.PointToScreen(new Point(0, 0)); var end = element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight));
        int left = (int)Math.Floor(start.X), top = (int)Math.Floor(start.Y), width = (int)Math.Ceiling(end.X) - left, height = (int)Math.Ceiling(end.Y) - top;
        bool current = ReferenceEquals(expected.Surface.LastRenderedFrame, expected.Frame);
        var visual = new DrawingVisual(); RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
        var area = new System.Windows.Rect(0, 0, element.ActualWidth, element.ActualHeight);
        var brush = new VisualBrush(element) { ViewboxUnits = BrushMappingMode.Absolute, Viewbox = area, Stretch = Stretch.Fill };
        using (var drawing = visual.RenderOpen())
        {
            drawing.PushTransform(new TranslateTransform(start.X - left, start.Y - top));
            drawing.PushTransform(new ScaleTransform(dpi.DpiScaleX, dpi.DpiScaleY)); drawing.DrawRectangle(brush, null, area); drawing.Pop(); drawing.Pop();
        }
        // Use synchronous WPF rendering as a reference/fence. Actual presentation
        // is verified below against GDI pixels; this image never replaces them.
        var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); image.Render(visual); image.Freeze();
        var pixels = new byte[width * height * 4]; image.CopyPixels(pixels, width * 4, 0);
        return new(left, top, width, height, pixels, image, current);
    }
    private static PreviewMatch Compare(PreviewReference expected, byte[] actual, Rect bounds, int width)
    {
        int changed = 0, total = 0, columns = (expected.Width + 3) / 4;
        var counts = new int[columns * ((expected.Height + 3) / 4)]; var changes = new int[counts.Length];
        if (expected.Left < bounds.Left || expected.Top < bounds.Top || expected.Left + expected.Width > bounds.Right || expected.Top + expected.Height > bounds.Bottom) return new(false, 1, 1);
        for (int y = 2; y < expected.Height - 2; y++) for (int x = 2; x < expected.Width - 2; x++)
        {
            int i = (y * expected.Width + x) * 4; if (expected.Pixels[i + 3] < 250) continue;
            int j = ((expected.Top - bounds.Top + y) * width + expected.Left - bounds.Left + x) * 4;
            bool differs = Math.Abs(expected.Pixels[i] - actual[j]) > 24 || Math.Abs(expected.Pixels[i + 1] - actual[j + 1]) > 24 || Math.Abs(expected.Pixels[i + 2] - actual[j + 2]) > 24;
            int tile = (y / 4) * columns + x / 4; total++; counts[tile]++;
            if (differs) { changed++; changes[tile]++; }
        }
        double fraction = total == 0 ? 1 : (double)changed / total;
        double maximum = counts.Select((count, i) => count >= 12 ? (double)changes[i] / count : 0).Max();
        return new(total > 100 && fraction < .01 && maximum <= .75, fraction, maximum);
    }
    // Always actual SRCCOPY|CAPTUREBLT pixels; never substitute the WPF reference.
    private static byte[] DesktopPixels(Rect bounds)
    {
        int width = bounds.Right - bounds.Left, height = bounds.Bottom - bounds.Top;
        var screen = GetDC(IntPtr.Zero); var memory = CreateCompatibleDC(screen); IntPtr bitmap = IntPtr.Zero, priorBitmap = IntPtr.Zero;
        var pixels = new byte[width * height * 4];
        try
        {
            var info = new BitmapInfo { Size = (uint)Marshal.SizeOf<BitmapInfo>(), Width = width, Height = -height, Planes = 1, BitCount = 32 };
            bitmap = CreateDIBSection(screen, ref info, 0, out var bits, IntPtr.Zero, 0);
            if (screen == IntPtr.Zero || memory == IntPtr.Zero || bitmap == IntPtr.Zero || bits == IntPtr.Zero) throw new InvalidOperationException("Screen buffer unavailable");
            priorBitmap = SelectObject(memory, bitmap);
            if (!BitBlt(memory, 0, 0, width, height, screen, bounds.Left, bounds.Top, 0x40CC0020)) throw new InvalidOperationException("Desktop pixel copy failed");
            Marshal.Copy(bits, pixels, 0, pixels.Length); return pixels;
        }
        finally
        {
            if (priorBitmap != IntPtr.Zero) SelectObject(memory, priorBitmap);
            if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
            if (memory != IntPtr.Zero) DeleteDC(memory);
            if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
        }
    }
    private static readonly List<object> records = [];
    private static string? Folder => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ? Environment.GetEnvironmentVariable("RUNNER_TEMP") : null;
    public static async Task CaptureAsync(Window window, string name, Action<bool, string> check, object? scenario = null, PreviewExpectation? expectedPreview = null)
    {
        if (Folder is not { } folder) return;
        if (name.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')) throw new ArgumentException("Screen evidence name");
        bool prior = window.Topmost;
        try
        {
            if (!window.IsVisible || window.WindowState == WindowState.Minimized) throw new InvalidOperationException("Screen target is not visible");
            window.SetCurrentValue(Window.TopmostProperty, true); window.Activate(); window.UpdateLayout();
            // A Normal-priority delay continuation can overtake a pending Render.
            // Submit the current frame before allowing the desktop compositor time.
            await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            PreviewReference? reference = null; var paintWait = Stopwatch.StartNew();
            if (expectedPreview != null)
            {
                while (!ReferenceEquals(expectedPreview.Surface.LastRenderedFrame, expectedPreview.Frame) && paintWait.Elapsed < TimeSpan.FromSeconds(2))
                {
                    await Task.Delay(16);
                    await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                }
                reference = Reference(expectedPreview);
            }
            else { var fence = new RenderTargetBitmap(1, 1, 96, 96, PixelFormats.Pbgra32); fence.Render(window); }
            await Task.Delay(100);
            await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);
            await Task.Run(() => Marshal.ThrowExceptionForHR(DwmFlush()));
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
            byte[] pixels; PreviewMatch? comparison = null; int attempts = 0; var wait = Stopwatch.StartNew();
            do
            {
                pixels = DesktopPixels(bounds); attempts++;
                if (reference == null) break;
                comparison = Compare(reference, pixels, bounds, width);
                if (comparison.Matches || wait.Elapsed >= TimeSpan.FromSeconds(2)) break;
                await Task.Delay(50); await Task.Run(() => Marshal.ThrowExceptionForHR(DwmFlush()));
            } while (true);
            object? presentation = null;
            if (reference != null)
            {
                var match = comparison ?? throw new InvalidOperationException("Preview comparison was not produced");
                bool matched = reference.FrameCurrent && match.Matches;
                presentation = new
                {
                    reference.FrameCurrent,
                    match.Matches,
                    match.ChangedFraction,
                    match.MaximumTileFraction,
                    attempts,
                    captureWaitMilliseconds = paintWait.Elapsed.TotalMilliseconds,
                    reference.Left,
                    reference.Top,
                    reference.Width,
                    reference.Height,
                    referencePixelSha256 = Convert.ToHexString(SHA256.HashData(reference.Pixels))
                };
                check(matched, "screen_phase_" + name);
                await Task.Run(() => { var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(reference.Image)); using var file = File.Create(Path.Combine(folder, "voxpet-runtime-preview-reference-" + name + ".png")); encoder.Save(file); });
            }
            var colors = new HashSet<int>();
            for (int i = 0; i < pixels.Length; i += 4) { pixels[i + 3] = 255; if (i % 64 == 0) colors.Add((pixels[i] << 16) | (pixels[i + 1] << 8) | pixels[i + 2]); }
            bool valid = colors.Count > 24 && GetForegroundWindow() == handle;
            var image = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4); image.Freeze();
            string fileName = "voxpet-screen-" + name + ".png";
            await Task.Run(() => { var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); using var file = File.Create(Path.Combine(folder, fileName)); encoder.Save(file); });
            var record = new { fileName, capture = "desktop-bitblt-captureblt", actualDesktopPixels = true, syntheticRender = false, privateAssets = false, os = Environment.OSVersion.VersionString, utc = DateTime.UtcNow, hwnd = handle.ToInt64(), window.Title, bounds.Left, bounds.Top, width, height, uniqueColors = colors.Count, foreground = GetForegroundWindow().ToInt64(), valid, pixelSha256 = Convert.ToHexString(SHA256.HashData(pixels)), scenario, presentation };
            records.Add(record);
            File.WriteAllText(Path.Combine(folder, "voxpet-screen-" + name + ".json"), JsonSerializer.Serialize(record, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(Path.Combine(folder, "voxpet-screen-index.json"), JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
            check(valid, "screen_" + name);
        }
        finally { window.SetCurrentValue(Window.TopmostProperty, prior); }
    }
}
