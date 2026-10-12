using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.Core.Models;

namespace VoxPet.App.Services;

/// <summary>캐릭터별512좌표에 맞춘 독자 벡터 작화. 사용자 PNG를 읽거나 원격에 전달하지 않는다.</summary>
internal static class CostumePresets
{
    internal static byte[] Draw(string id, CostumeCategory category)
    {
        var rig = CharacterRig.Default(id);
        bool side = id == "pelly", human = id is "neulbo-man" or "neulbo-girl";
        Color color = id switch { "drawing-neulbo" => Color.FromRgb(72, 119, 92), "neulbo-man" => Color.FromRgb(79, 113, 156), "neulbo-girl" => Color.FromRgb(183, 100, 127), "pelly" => Color.FromRgb(80, 160, 179), _ => Color.FromRgb(112, 128, 83) };
        var fabric = new SolidColorBrush(color); fabric.Freeze();
        var light = new SolidColorBrush(Color.FromRgb((byte)((color.R + 255) / 2), (byte)((color.G + 255) / 2), (byte)((color.B + 255) / 2))); light.Freeze();
        var outline = new Pen(Brushes.SaddleBrown, 3);
        var visual = new DrawingVisual();
        using (var d = visual.RenderOpen())
        {
            void Path(string path, Brush brush) => d.DrawGeometry(brush, outline, Geometry.Parse(path));
            void Star(double x, double y, double size)
            {
                d.PushTransform(new TranslateTransform(x, y)); d.PushTransform(new ScaleTransform(size, size));
                Path("M0,-18 L5,-6 L18,-5 L8,4 L11,17 L0,10 L-11,17 L-8,4 L-18,-5 L-5,-6 Z", Brushes.Gold); d.Pop(); d.Pop();
            }
            double headX = side ? 318 : 256, eyeY = human ? 155 : side ? 166 : 205;
            double bodyTop = rig.HeadCutY + 12, bodyBottom = human ? 385 : side ? 414 : 438;
            double left = rig.Left.X + rig.Left.Width * .7, right = rig.Right.X + rig.Right.Width * .3;
            void Shirt(bool coat, bool dress)
            {
                // 끝단은 실제 캐릭터 팔 영역 안에 두어 원본과 동일한 변형에 연결한다.
                foreach (var limb in new[] { rig.Left, rig.Right }.Where(limb => limb.Enabled))
                {
                    d.DrawRoundedRectangle(fabric, outline, new Rect(limb.X + 4, limb.Y + 8, limb.Width - 8, limb.Height * .78), 18, 18);
                    d.DrawRoundedRectangle(light, outline, new Rect(limb.X + 6, limb.Y + limb.Height * .7, limb.Width - 12, 14), 4, 4);
                }
                if (dress) Path($"M{left},{bodyTop} L{right},{bodyTop} L{right + 25},{bodyBottom + 20} Q256,{bodyBottom + 38} {left - 25},{bodyBottom + 20} Z", fabric);
                else d.DrawRoundedRectangle(fabric, outline, new Rect(left, bodyTop, Math.Max(36, right - left), bodyBottom - bodyTop), 17, 17);
                Path($"M{left + 20},{bodyTop} L256,{bodyTop + 32} L{right - 20},{bodyTop} L{right - 28},{bodyTop + 10} L256,{bodyTop + 45} L{left + 28},{bodyTop + 10} Z", light);
                d.DrawLine(outline, new Point(256, bodyTop + 40), new Point(256, bodyBottom - 10));
                for (int i = 0; i < 3; i++) d.DrawEllipse(Brushes.Gold, outline, new Point(256, bodyTop + 54 + i * 21), 4, 4);
                if (coat)
                    foreach (double x in new[] { left + 10, right - 39 })
                    { d.DrawRoundedRectangle(light, outline, new Rect(x, bodyBottom - 43, 29, 25), 4, 4); d.DrawLine(outline, new Point(x, bodyBottom - 39), new Point(x + 29, bodyBottom - 39)); }
                if (dress) Star(256, bodyBottom - 35, .6);
            }
            switch (category)
            {
                case CostumeCategory.Hat:
                    d.DrawEllipse(fabric, outline, new Point(headX, human ? 53 : 71), side ? 62 : 88, 27);
                    d.DrawRoundedRectangle(light, outline, new Rect(headX - 58, human ? 64 : 82, 116, 15), 6, 6);
                    d.DrawLine(new Pen(fabric, 8), new Point(headX + 14, human ? 27 : 45), new Point(headX + 26, human ? 17 : 35)); Star(headX + 37, human ? 50 : 68, .45); break;
                case CostumeCategory.Glasses:
                    if (side) { d.DrawEllipse(new SolidColorBrush(Color.FromArgb(45, 190, 231, 255)), outline, new Point(headX, eyeY), 31, 30); d.DrawLine(outline, new Point(headX + 29, eyeY), new Point(headX + 65, eyeY - 8)); }
                    else
                    {
                        foreach (double x in new[] { headX - (human ? 36 : 54), headX + (human ? 36 : 54) }) d.DrawEllipse(new SolidColorBrush(Color.FromArgb(45, 190, 231, 255)), outline, new Point(x, eyeY), human ? 29 : 43, human ? 24 : 39);
                        d.DrawLine(outline, new Point(headX - 12, eyeY), new Point(headX + 12, eyeY));
                    }
                    break;
                case CostumeCategory.FaceDecoration:
                    double ribbonY = side ? 242 : human ? 200 : 283, ribbonX = side ? 320 : 256;
                    d.PushTransform(new TranslateTransform(ribbonX, ribbonY));
                    Path("M0,0 C-55,-35 -52,29 -7,7 Z M0,0 C55,-35 52,29 7,7 Z", fabric); d.DrawEllipse(Brushes.Gold, outline, new Point(0, 0), 8, 8); d.Pop(); break;
                case CostumeCategory.Earrings:
                    foreach (double x in side ? new[] { headX + 58 } : new[] { headX - (human ? 77 : 109), headX + (human ? 77 : 109) })
                    { d.DrawLine(outline, new Point(x, eyeY + 20), new Point(x, eyeY + 46)); Star(x, eyeY + 54, .55); }
                    break;
                case CostumeCategory.Top: Shirt(false, false); break;
                case CostumeCategory.Outerwear: Shirt(true, false); break;
                case CostumeCategory.OnePiece: Shirt(false, true); break;
                case CostumeCategory.Bottom:
                    double top = human ? 379 : side ? 367 : 420, bottom = human ? 472 : side ? 418 : 464;
                    Path($"M{left},{top} L{right},{top} L{right - 6},{bottom} L267,{bottom} L256,{top + 26} L245,{bottom} L{left + 6},{bottom} Z", fabric);
                    d.DrawRoundedRectangle(light, outline, new Rect(left, top, Math.Max(36, right - left), 15), 4, 4);
                    d.DrawLine(new Pen(light, 8), new Point(left + 15, top), new Point(left + 30, top - 38)); d.DrawLine(new Pen(light, 8), new Point(right - 15, top), new Point(right - 30, top - 38)); break;
                case CostumeCategory.Cape:
                    Path($"M190,{rig.HeadCutY - 12} Q256,{rig.HeadCutY + 20} 322,{rig.HeadCutY - 12} L390,450 Q256,487 122,450 Z", fabric);
                    foreach (var (x, y) in new[] { (180d, 377d), (331d, 390d), (255d, 438d) }) Star(x, y, .65); break;
                default: throw new ArgumentOutOfRangeException(nameof(category));
            }
        }
        var image = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32); image.Render(visual); image.Freeze();
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); using var stream = new MemoryStream(); encoder.Save(stream); return stream.ToArray();
    }
}
