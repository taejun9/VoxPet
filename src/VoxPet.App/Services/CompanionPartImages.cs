using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Legacy;

namespace VoxPet.App.Services;

/// <summary>원본을 변경하지 않고 선택/표정 변경 때만 512px frozen 파츠를 만든다.</summary>
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
        body = Geometry.Combine(body, head, GeometryCombineMode.Exclude, null);
        foreach (string leg in new[] { "leg-left", "leg-right" }) body = Geometry.Combine(body, parts[leg], GeometryCombineMode.Exclude, null);
        var result = parts.ToDictionary(p => p.Key, p => (Geometry)p.Value); result.Add("head", head); result.Add("body", body);
        foreach (var shape in result.Values) shape.Freeze(); masks.Add(side, result); return result;
    }
    private static Color Skin(ImageSource source, bool side)
    {
        if (source is not BitmapSource pixels) return side ? Color.FromRgb(255, 244, 223) : Color.FromRgb(205, 169, 138);
        var bitmap = new FormatConvertedBitmap(pixels, PixelFormats.Bgra32, null, 0); var pixel = new byte[4];
        bitmap.CopyPixels(new Int32Rect((int)(bitmap.PixelWidth * .5), (int)(bitmap.PixelHeight * (side ? .69 : .60)), 1, 1), pixel, 4, 0);
        return Color.FromRgb(pixel[2], pixel[1], pixel[0]);
    }
    private static RenderTargetBitmap Raster(Action<DrawingContext> draw)
    {
        var visual = new DrawingVisual(); using (var drawing = visual.RenderOpen()) draw(drawing);
        var image = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32); image.Render(visual); image.Freeze(); return image;
    }
    private static byte[] Pixels(BitmapSource image)
    { var pixels = new byte[512 * 512 * 4]; image.CopyPixels(pixels, 512 * 4, 0); return pixels; }
    private static (double X, double Y) LowerBody(ImageSource source)
    {
        if (source is not BitmapSource bitmap) return (0, 0);
        var bgra = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0); int width = bgra.PixelWidth, height = bgra.PixelHeight;
        var pixels = new byte[width * height * 4]; bgra.CopyPixels(pixels, width * 4, 0); int left = width, right = -1, bottom = -1;
        for (int y = (int)(height * .65); y < height; y++) for (int x = 0; x < width; x++)
            if (pixels[(y * width + x) * 4 + 3] > 32) { left = Math.Min(left, x); right = Math.Max(right, x); bottom = Math.Max(bottom, y); }
        return right < 0 ? (0, 0) : ((left + right) * 256d / width, bottom * 512d / height);
    }
    internal static ImageSource Cut(ImageSource original, string id, bool side)
    {
        // Adjacent antialiased cuts leave a translucent seam after source-over.
        // Sample the same original pixels in a 6px joint overlap instead.
        // The original PNG alpha still defines every outer silhouette edge.
        var boundary = Masks(side)[id];
        var extension = boundary.GetWidenedPathGeometry(new Pen(Brushes.White, 12));
        // The torso may overlap only inside its reconstructed silhouette. Retaining
        // an old wing-tip outline here leaves a stationary ghost during a fold.
        Geometry extra = id == "body" ? Geometry.Combine(extension, Geometry.Parse(CompanionParts.BodyContour(side)), GeometryCombineMode.Intersect, null) : extension;
        var overlap = Geometry.Combine(boundary, extra, GeometryCombineMode.Union, null);
        var cut = new DrawingGroup { ClipGeometry = overlap };
        cut.Children.Add(new ImageDrawing(original, new Rect(0, 0, 512, 512)));
        // Fixed canvas bounds keep WPF from stretching a cropped arm/head to 512px.
        var canvas = new DrawingGroup();
        canvas.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 512, 512))));
        var skin = new SolidColorBrush(Skin(original, side));
        if (id == "body")
        {
            // Reconstruct the torso hidden under the original arms. A clipped-out
            // arm must not leave a transparent hole when its independent bone moves.
            var outline = new Pen(new SolidColorBrush(Color.FromRgb(65, 34, 18)), 5);
            var underlay = Pixels(Raster(d => d.DrawGeometry(skin, outline, Geometry.Parse(CompanionParts.BodyContour(side)))));
            var source = Pixels(Raster(d => d.DrawImage(original, new Rect(0, 0, 512, 512))));
            var arms = Pixels(Raster(d => { foreach (string arm in new[] { "arm-left", "arm-right" }) d.DrawGeometry(Brushes.White, null, Masks(side)[arm]); }));
            var legs = Pixels(Raster(d => { foreach (string leg in new[] { "leg-left", "leg-right" }) d.DrawGeometry(Brushes.White, null, Masks(side)[leg]); }));
            // Explicit premultiplied alpha avoids WPF's nested vector opacity-mask
            // bounds artifacts. Only the original arm footprint may reveal backing.
            for (int pixel = 0; pixel < underlay.Length; pixel += 4)
            {
                double alpha = source[pixel + 3] / 255d * arms[pixel + 3] / 255 * (1 - legs[pixel + 3] / 255d);
                for (int channel = 0; channel < 4; channel++) underlay[pixel + channel] = (byte)Math.Round(underlay[pixel + channel] * alpha);
            }
            var backing = BitmapSource.Create(512, 512, 96, 96, PixelFormats.Pbgra32, null, underlay, 512 * 4); backing.Freeze();
            canvas.Children.Add(new ImageDrawing(backing, new Rect(0, 0, 512, 512)));
        }
        canvas.Children.Add(cut); canvas.Freeze();
        // Rasterize absolute Canvas coordinates once. DrawingImage ink bounds can
        // otherwise normalize clipped regions differently in desktop/bitmap draws.
        return Raster(d => d.DrawDrawing(canvas));
    }
    internal static IReadOnlyDictionary<string, ImageSource> Create(ImageSource[,] sheet, bool side)
    {
        var result = new Dictionary<string, ImageSource>();
        foreach (string id in CompanionParts.Ids.Where(id => id != "head")) result.Add(CompanionParts.Asset(id), Cut(sheet[0, 1], id, side));
        foreach (string hand in new[] { "left", "right" })
        {
            var arm = result[CompanionParts.Asset("arm-" + hand)];
            if (side && hand == "left")
            {
                var rightWing = result[CompanionParts.Asset("arm-right")];
                // The hidden far wing is reconstructed locally from the original
                // feather pixels. It is only selected by two-hand action sprites.
                arm = Raster(d => { d.PushTransform(new MatrixTransform(-.55, 0, 0, .55, 420.35, 205.9)); d.DrawImage(rightWing, new Rect(0, 0, 512, 512)); d.Pop(); });
            }
            var sourceArm = arm;
            result.Add(CompanionParts.GripArm(hand), side ? sourceArm : Raster(d =>
            { d.PushClip(new RectangleGeometry(new Rect(0, 0, 512, 422))); d.DrawImage(sourceArm, new Rect(0, 0, 512, 512)); d.Pop(); }));
        }
        var reference = LowerBody(sheet[0, 1]);
        for (int m = 0; m < sheet.GetLength(0); m++) for (int e = 0; e < 2; e++)
        {
            var source = sheet[m, e]; var body = LowerBody(source);
            double x = reference.X - body.X, y = reference.Y - body.Y;
            if (x != 0 || y != 0) source = Raster(d => { d.PushTransform(new TranslateTransform(x, y)); d.DrawImage(sheet[m, e], new Rect(0, 0, 512, 512)); d.Pop(); });
            result.Add(CompanionParts.Head(m, e), Cut(source, "head", side));
        }
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
