using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.CompilerServices;
using VoxPet.Core.Character;
using VoxPet.Core.Rendering;

namespace VoxPet.App.Views;

/// <summary>최종 frame의 순서/affine/opacity/mesh를 그대로 그린다. 입·동작·상속을 결정하지 않는다.</summary>
internal static class RenderFrameDrawing
{
    internal readonly record struct DrawStatistics(int CandidateTriangles, int DrawnTriangles, int NewClips, int AlphaScans);
    private readonly record struct ClipKey(RigPoint A, RigPoint B, RigPoint C, bool Joined);
    private sealed record AlphaArea(Rect Bounds);
    private static readonly ConditionalWeakTable<BitmapSource, AlphaArea> alphaAreas = new();
    [ThreadStatic] private static Dictionary<ClipKey, StreamGeometry>? clips;
    private static AlphaArea Scan(BitmapSource image)
    {
        int width = image.PixelWidth, height = image.PixelHeight;
        if (image.Format != PixelFormats.Bgra32 && image.Format != PixelFormats.Pbgra32) return new(new Rect(0, 0, 1, 1));
        var pixels = new byte[checked(width * height * 4)]; image.CopyPixels(pixels, width * 4, 0);
        int left = width, right = -1, top = height, bottom = -1;
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            if (pixels[(y * width + x) * 4 + 3] != 0)
            { left = Math.Min(left, x); right = Math.Max(right, x); top = Math.Min(top, y); bottom = Math.Max(bottom, y); }
        // Include the entire border texel and its filtering footprint.
        return new(right < 0 ? Rect.Empty : new Rect(Math.Max(0, left - 1d) / width, Math.Max(0, top - 1d) / height,
            (Math.Min(width, right + 2d) - Math.Max(0, left - 1d)) / width, (Math.Min(height, bottom + 2d) - Math.Max(0, top - 1d)) / height));
    }
    private static Rect SourceBounds(ImageSource image, RenderLayer layer, ref int scans)
    {
        Rect area = new(0, 0, 1, 1);
        if (image is BitmapSource bitmap)
        {
            if (!alphaAreas.TryGetValue(bitmap, out var known)) { known = alphaAreas.GetValue(bitmap, Scan); scans++; }
            area = known.Bounds;
        }
        if (area.IsEmpty) return area;
        var placed = new Rect(area.X * layer.Width, area.Y * layer.Height, area.Width * layer.Width, area.Height * layer.Height);
        placed.Transform(Matrix(layer.Matrix)); return placed;
    }
    internal static Matrix Matrix(AffineTransform2D value) => new(value.M11, value.M12, value.M21, value.M22, value.OffsetX, value.OffsetY);
    private static (Point A, Point B, Point C) Joined(RenderTriangle triangle)
    {
        var a = new Point(triangle.A.X, triangle.A.Y); var b = new Point(triangle.B.X, triangle.B.Y); var c = new Point(triangle.C.X, triangle.C.Y);
        double la = (b - c).Length, lb = (a - c).Length, lc = (a - b).Length, perimeter = la + lb + lc;
        double area2 = Math.Abs((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X));
        if (area2 < .001) return (a, b, c);
        var center = new Point((la * a.X + lb * b.X + lc * c.X) / perimeter, (la * a.Y + lb * b.Y + lc * c.Y) / perimeter);
        // A half-pixel overlap hides texture-clip rasterization cracks. Shared UVs
        // meet on the same edge; the source PNG alpha keeps the outer silhouette.
        double amount = 1 + .5 * perimeter / area2;
        Point Expand(Point point) => center + (point - center) * amount;
        return (Expand(a), Expand(b), Expand(c));
    }
    internal static DrawStatistics Draw(DrawingContext drawing, RenderFrame frame, IReadOnlyDictionary<string, ImageSource> images, bool props = true, double width = 512, double height = 512, bool optimize = true)
    {
        int candidates = 0, drawn = 0, created = 0, scans = 0;
        double scale = Math.Min(width / frame.CanvasWidth, height / frame.CanvasHeight);
        drawing.PushTransform(new TranslateTransform((width - scale * frame.CanvasWidth) / 2, (height - scale * frame.CanvasHeight) / 2));
        drawing.PushTransform(new ScaleTransform(scale, scale));
        drawing.PushClip(new RectangleGeometry(new Rect(0, 0, frame.CanvasWidth, frame.CanvasHeight)));
        foreach (var layer in frame.Layers)
        {
            if (!props && layer.Id.StartsWith("legacy-prop-", StringComparison.Ordinal)) continue;
            if (!images.TryGetValue(layer.Asset, out var image)) throw new InvalidOperationException("준비되지 않은 Runtime asset: " + layer.Asset);
            Rect bounds = optimize ? SourceBounds(image, layer, ref scans) : new Rect(0, 0, frame.CanvasWidth, frame.CanvasHeight);
            if (optimize && bounds.IsEmpty) continue;
            if (image is BitmapSource { PixelWidth: 1, PixelHeight: 1 } pixel && (pixel.Format == PixelFormats.Bgra32 || pixel.Format == PixelFormats.Pbgra32))
            {
                var rgba = new byte[4]; pixel.CopyPixels(rgba, 4, 0);
                if (rgba[3] == 0) continue;
            }
            drawing.PushOpacity(layer.Opacity);
            if (layer.Mesh is null)
            {
                drawing.PushTransform(new MatrixTransform(Matrix(layer.Matrix)));
                drawing.DrawImage(image, new Rect(0, 0, layer.Width, layer.Height)); drawing.Pop();
            }
            else foreach (var triangle in layer.Mesh.Triangles)
            {
                candidates++;
                var footprint = layer.Mesh.JoinEdges ? Joined(triangle) : (A: new Point(triangle.A.X, triangle.A.Y), B: new Point(triangle.B.X, triangle.B.Y), C: new Point(triangle.C.X, triangle.C.Y));
                var extent = new Rect(new Point(Math.Min(footprint.A.X, Math.Min(footprint.B.X, footprint.C.X)) - 1, Math.Min(footprint.A.Y, Math.Min(footprint.B.Y, footprint.C.Y)) - 1),
                    new Point(Math.Max(footprint.A.X, Math.Max(footprint.B.X, footprint.C.X)) + 1, Math.Max(footprint.A.Y, Math.Max(footprint.B.Y, footprint.C.Y)) + 1));
                if (optimize && !extent.IntersectsWith(bounds)) continue;
                drawn++;
                // mesh는 Canvas 좌표 clip/transform이다. layer matrix는 원본 이미지 배치다.
                drawing.PushTransform(new MatrixTransform(Matrix(triangle.Matrix)));
                clips ??= [];
                var key = new ClipKey(triangle.A, triangle.B, triangle.C, layer.Mesh.JoinEdges);
                if (!optimize || !clips.TryGetValue(key, out var clip))
                {
                    clip = new StreamGeometry();
                    using (var context = clip.Open())
                    {
                        var joined = layer.Mesh.JoinEdges ? Joined(triangle) : (A: new Point(triangle.A.X, triangle.A.Y), B: new Point(triangle.B.X, triangle.B.Y), C: new Point(triangle.C.X, triangle.C.Y));
                        context.BeginFigure(joined.A, true, true);
                        context.LineTo(joined.B, true, false);
                        context.LineTo(joined.C, true, false);
                    }
                    clip.Freeze(); created++;
                    if (optimize) { if (clips.Count >= 4096) clips.Clear(); clips[key] = clip; }
                }
                drawing.PushClip(clip); drawing.PushTransform(new MatrixTransform(Matrix(layer.Matrix)));
                drawing.DrawImage(image, new Rect(0, 0, layer.Width, layer.Height)); drawing.Pop(); drawing.Pop(); drawing.Pop();
            }
            drawing.Pop();
        }
        drawing.Pop(); drawing.Pop(); drawing.Pop();
        return new(candidates, drawn, created, scans);
    }
}
