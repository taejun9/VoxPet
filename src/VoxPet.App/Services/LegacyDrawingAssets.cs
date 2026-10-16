using System.Windows;
using System.Windows.Media;
using VoxPet.Core.Models;

namespace VoxPet.App.Services;

/// <summary>한 번 준비하는 opaque 그림. 위치·선택·opacity는 Core frame이 결정한다.</summary>
internal static class LegacyDrawingAssets
{
    internal static readonly IReadOnlyDictionary<string, ImageSource> All = Create();
    internal static ImageSource Guides(CharacterRig rig)
    {
        var group = new DrawingGroup(); using (var drawing = group.Open())
        {
            drawing.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 512, 512));
            drawing.DrawLine(new Pen(Brushes.Gold, 2), new Point(0, rig.HeadCutY), new Point(512, rig.HeadCutY));
            foreach (var limb in new[] { rig.Left, rig.Right })
            {
                drawing.DrawRectangle(null, new Pen(limb.Enabled ? Brushes.Cyan : Brushes.Gray, 2), new Rect(limb.X, limb.Y, limb.Width, limb.Height));
                drawing.DrawEllipse(Brushes.Red, null, new Point(limb.PivotX, limb.PivotY), 4, 4);
            }
        }
        group.Freeze(); var image = new DrawingImage(group); image.Freeze(); return image;
    }
    private static IReadOnlyDictionary<string, ImageSource> Create()
    {
        var images = new Dictionary<string, ImageSource>();
        void Image(string name, double origin, Action<DrawingContext> draw)
        {
            var group = new DrawingGroup(); using (var context = group.Open())
            {
                double extent = name.Contains("tear") ? 20 : 200;
                context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, name.Contains("tear") ? 10 : 200, extent));
                context.PushTransform(new TranslateTransform(origin, origin)); draw(context); context.Pop();
            }
            group.Freeze(); var image = new DrawingImage(group); image.Freeze(); images.Add(name, image);
        }
        static Geometry Shape(string path) => Geometry.Parse(path);
        var outline = new Pen(Brushes.SaddleBrown, 3); outline.Freeze();
        Image("legacy/tear.png", 0, c => c.DrawGeometry(Brushes.LightSkyBlue, null, Shape("M5,0 C4,5 0,8 0,12 C0,20 10,20 10,12 C10,8 6,5 5,0 Z")));
        foreach (var gesture in Enum.GetValues<HandGesture>()) Image("legacy/prop-" + gesture + ".png", 100, c =>
        {
            switch (gesture)
            {
                case HandGesture.Heart: c.DrawGeometry(Brushes.HotPink, outline, Shape("M0,8 C-32,-14 -29,-36 -12,-35 C-4,-35 0,-29 0,-25 C0,-29 4,-35 12,-35 C29,-36 32,-14 0,8 Z")); break;
                case HandGesture.ToyGun:
                    c.DrawGeometry(Brushes.DeepSkyBlue, outline, Shape("M0,0 L0,-18 L65,-18 L65,-4 L28,-4 L22,20 L8,20 L13,0 Z")); c.DrawRectangle(Brushes.Orange, null, new Rect(60, -18, 8, 14)); break;
                case HandGesture.ToySword: c.DrawGeometry(Brushes.LightBlue, outline, Shape("M-5,0 L-5,-63 L0,-78 L5,-63 L5,0 Z M-18,0 L18,0 L18,7 L-18,7 Z M-4,7 L4,7 L4,28 L-4,28 Z")); break;
                case HandGesture.MagicWand:
                    c.DrawRoundedRectangle(Brushes.MediumPurple, outline, new Rect(-4, -68, 8, 80), 3, 3);
                    c.PushTransform(new TranslateTransform(0, -76)); c.DrawGeometry(Brushes.Gold, outline, Shape("M0,-18 L5,-6 L18,-5 L8,4 L11,17 L0,10 L-11,17 L-8,4 L-18,-5 L-5,-6 Z")); c.Pop(); break;
                case HandGesture.Claw:
                    c.DrawGeometry(Brushes.Wheat, outline, Shape("M-20,10 L-24,-15 Q-40,-24 -32,-33 Q-26,-39 -16,-29 L-12,-25 V-65 Q-12,-78 -3,-78 Q6,-78 6,-65 V-30 Q15,-37 23,-29 Q33,-34 39,-25 L42,-5 Q39,14 21,20 L19,30 H-17 Z"));
                    c.DrawRoundedRectangle(Brushes.LightSeaGreen, outline, new Rect(-22, 19, 45, 14), 4, 4); break;
            }
        });
        return images;
    }
}
