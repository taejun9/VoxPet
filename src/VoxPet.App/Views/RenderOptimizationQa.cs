using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Rendering;

namespace VoxPet.App.Views;

/// <summary>Compare the optimized draw with the original full-grid drawing, using synthetic/public pixels.</summary>
internal static class RenderOptimizationQa
{
    private static (byte[] Pixels, RenderFrameDrawing.DrawStatistics Stats) Raster(RenderFrame frame, IReadOnlyDictionary<string, ImageSource> images, int size, bool optimize)
    {
        var visual = new DrawingVisual(); RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased); RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
        RenderFrameDrawing.DrawStatistics stats;
        using (var drawing = visual.RenderOpen()) stats = RenderFrameDrawing.Draw(drawing, frame, images, width: size, height: size, optimize: optimize);
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
        var pixels = new byte[size * size * 4]; bitmap.CopyPixels(pixels, size * 4, 0); return (pixels, stats);
    }
    internal static void Run(RenderFrame frame, IReadOnlyDictionary<string, ImageSource> images, string id, Action<bool, string> check)
    {
        var records = new List<object>();
        foreach (int size in new[] { 128, 256, 512 })
        {
            var old = Raster(frame, images, size, false); var current = Raster(frame, images, size, true); var warm = Raster(frame, images, size, true);
            check(old.Pixels.SequenceEqual(current.Pixels) && old.Pixels.SequenceEqual(warm.Pixels), $"avatar_render_pixels_exact_{id}_{size}");
            check(current.Stats.CandidateTriangles > 0 && current.Stats.DrawnTriangles < current.Stats.CandidateTriangles, $"avatar_render_transparent_triangles_culled_{id}_{size}");
            check(warm.Stats.NewClips == 0 && warm.Stats.AlphaScans == 0, $"avatar_render_warm_cache_no_geometry_or_scan_{id}_{size}");
            check(warm.Stats.NewMatrices == 0, $"avatar_render_warm_cache_no_matrix_allocation_{id}_{size}");
            records.Add(new { size, old = old.Stats, current = current.Stats, warm = warm.Stats });
        }
        double Measure(bool optimize)
        {
            for (int i = 0; i < 2; i++) Raster(frame, images, 256, optimize);
            var elapsed = Stopwatch.StartNew();
            for (int i = 0; i < 8; i++) Raster(frame, images, 256, optimize);
            return elapsed.Elapsed.TotalMilliseconds / 8;
        }
        double baselineMs = Measure(false), optimizedMs = Measure(true);
        if (Environment.GetEnvironmentVariable("RUNNER_TEMP") is { } folder)
            File.WriteAllText(Path.Combine(folder, "voxpet-runtime-avatar-render-" + id + ".json"), JsonSerializer.Serialize(new { records, baselineMs, optimizedMs, timingIsMeasurementNotFpsGuarantee = true }));
    }
}
