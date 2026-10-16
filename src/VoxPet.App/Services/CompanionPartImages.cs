using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Legacy;

namespace VoxPet.App.Services;

/// <summary>원본을 변경하지 않고 frozen 파츠를 만든다. 분리 그림은 원본 픽셀을 공유한다.</summary>
internal static class CompanionPartImages
{
    private static readonly Dictionary<bool, IReadOnlyDictionary<string, Geometry>> masks = [];
    private static IReadOnlyDictionary<string, Geometry> Masks(bool side)
    {
        if (masks.TryGetValue(side, out var ready)) return ready;
        var head = Geometry.Parse(CompanionParts.HeadContour(side));
        var parts = CompanionParts.Ids.Where(id => id is not ("head" or "body")).ToDictionary(id => id, id => Geometry.Combine(Geometry.Parse(CompanionParts.Contour(id, side)), head, GeometryCombineMode.Exclude, null));
        Geometry body = Geometry.Combine(new RectangleGeometry(new Rect(0, 0, 512, 512)), head, GeometryCombineMode.Exclude, null);
        foreach (var shape in parts.Values) body = Geometry.Combine(body, shape, GeometryCombineMode.Exclude, null);
        var result = parts.ToDictionary(p => p.Key, p => (Geometry)p.Value); result.Add("head", head); result.Add("body", body);
        foreach (var shape in result.Values) shape.Freeze(); masks.Add(side, result); return result;
    }
    internal static ImageSource Cut(ImageSource original, string id, bool side)
    {
        var cut = new DrawingGroup { ClipGeometry = Masks(side)[id] };
        cut.Children.Add(new ImageDrawing(original, new Rect(0, 0, 512, 512)));
        // Fixed canvas bounds keep WPF from stretching a cropped arm/head to 512px.
        var canvas = new DrawingGroup();
        canvas.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 512, 512))));
        canvas.Children.Add(cut); canvas.Freeze(); var image = new DrawingImage(canvas); image.Freeze(); return image;
    }
    internal static IReadOnlyDictionary<string, ImageSource> Create(ImageSource[,] sheet, bool side)
    {
        var result = new Dictionary<string, ImageSource>();
        foreach (string id in CompanionParts.Ids.Where(id => id != "head")) result.Add(CompanionParts.Asset(id), Cut(sheet[0, 1], id, side));
        for (int m = 0; m < sheet.GetLength(0); m++) for (int e = 0; e < 2; e++) result.Add(CompanionParts.Head(m, e), Cut(sheet[m, e], "head", side));
        return result;
    }
    internal static byte[] Encode(ImageSource source)
    {
        BitmapSource bitmap;
        if (source is BitmapSource pixels) bitmap = pixels;
        else
        {
            var visual = new DrawingVisual(); using (var drawing = visual.RenderOpen()) drawing.DrawImage(source, new Rect(0, 0, 512, 512));
            var rendered = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32); rendered.Render(visual); rendered.Freeze(); bitmap = rendered;
        }
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var buffer = new System.IO.MemoryStream(); encoder.Save(buffer); return buffer.ToArray();
    }
}
