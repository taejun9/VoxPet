using System.Windows;
using System.Windows.Media;
using VoxPet.Core.Character;
using VoxPet.Core.Rendering;

namespace VoxPet.App.Views;

/// <summary>최종 frame의 순서/affine/opacity/mesh를 그대로 그린다. 입·동작·상속을 결정하지 않는다.</summary>
internal static class RenderFrameDrawing
{
    internal static Matrix Matrix(AffineTransform2D value) => new(value.M11, value.M12, value.M21, value.M22, value.OffsetX, value.OffsetY);
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
                    context.BeginFigure(new Point(triangle.A.X, triangle.A.Y), true, true);
                    context.LineTo(new Point(triangle.B.X, triangle.B.Y), true, false);
                    context.LineTo(new Point(triangle.C.X, triangle.C.Y), true, false);
                }
                drawing.PushClip(clip); drawing.PushTransform(new MatrixTransform(Matrix(layer.Matrix)));
                drawing.DrawImage(image, new Rect(0, 0, layer.Width, layer.Height)); drawing.Pop(); drawing.Pop(); drawing.Pop();
            }
            drawing.Pop();
        }
        drawing.Pop(); drawing.Pop(); drawing.Pop();
    }
}
