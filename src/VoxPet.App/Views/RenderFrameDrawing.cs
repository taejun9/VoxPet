using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Character;
using VoxPet.Core.Rendering;

namespace VoxPet.App.Views;

/// <summary>최종 frame의 순서/affine/opacity/mesh를 그대로 그린다. 입·동작·상속을 결정하지 않는다.</summary>
internal static class RenderFrameDrawing
{
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
    internal static void Draw(DrawingContext drawing, RenderFrame frame, IReadOnlyDictionary<string, ImageSource> images, bool props = true, double width = 512, double height = 512)
    {
        double scale = Math.Min(width / frame.CanvasWidth, height / frame.CanvasHeight);
        drawing.PushTransform(new TranslateTransform((width - scale * frame.CanvasWidth) / 2, (height - scale * frame.CanvasHeight) / 2));
        drawing.PushTransform(new ScaleTransform(scale, scale));
        drawing.PushClip(new RectangleGeometry(new Rect(0, 0, frame.CanvasWidth, frame.CanvasHeight)));
        foreach (var layer in frame.Layers)
        {
            if (!props && layer.Id.StartsWith("legacy-prop-", StringComparison.Ordinal)) continue;
            if (!images.TryGetValue(layer.Asset, out var image)) throw new InvalidOperationException("준비되지 않은 Runtime asset: " + layer.Asset);
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
                // mesh는 Canvas 좌표 clip/transform이다. layer matrix는 원본 이미지 배치다.
                drawing.PushTransform(new MatrixTransform(Matrix(triangle.Matrix)));
                var clip = new StreamGeometry();
                using (var context = clip.Open())
                {
                    var joined = layer.Mesh.JoinEdges ? Joined(triangle) : (A: new Point(triangle.A.X, triangle.A.Y), B: new Point(triangle.B.X, triangle.B.Y), C: new Point(triangle.C.X, triangle.C.Y));
                    context.BeginFigure(joined.A, true, true);
                    context.LineTo(joined.B, true, false);
                    context.LineTo(joined.C, true, false);
                }
                drawing.PushClip(clip); drawing.PushTransform(new MatrixTransform(Matrix(layer.Matrix)));
                drawing.DrawImage(image, new Rect(0, 0, layer.Width, layer.Height)); drawing.Pop(); drawing.Pop(); drawing.Pop();
            }
            drawing.Pop();
        }
        drawing.Pop(); drawing.Pop(); drawing.Pop();
    }
}
