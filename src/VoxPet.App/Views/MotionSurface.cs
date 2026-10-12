using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.Views;

/// <summary>원본 픽셀을 비파괴로 나눠 렌더한다. 의상 소매도 캐릭터와 동일한 영역/관절 변환을 사용한다.</summary>
public sealed class MotionSurface : FrameworkElement
{
    private CharacterViewModel? model;
    private CharacterRig? cachedRig;
    private MotionPose? cachedPose;
    private readonly Dictionary<(bool Head, double Follow), (Geometry Clip, Transform Transform)[]> meshes = [];
    private static readonly Rect CanvasRect = new(0, 0, 512, 512);
    private static readonly Geometry Tear = FrozenGeometry("M5,0 C4,5 0,8 0,12 C0,20 10,20 10,12 C10,8 6,5 5,0 Z");
    private static readonly Geometry Heart = FrozenGeometry("M0,8 C-32,-14 -29,-36 -12,-35 C-4,-35 0,-29 0,-25 C0,-29 4,-35 12,-35 C29,-36 32,-14 0,8 Z");
    private static readonly Geometry Gun = FrozenGeometry("M0,0 L0,-18 L65,-18 L65,-4 L28,-4 L22,20 L8,20 L13,0 Z");
    private static readonly Geometry Sword = FrozenGeometry("M-5,0 L-5,-63 L0,-78 L5,-63 L5,0 Z M-18,0 L18,0 L18,7 L-18,7 Z M-4,7 L4,7 L4,28 L-4,28 Z");
    private static readonly Geometry Star = FrozenGeometry("M0,-18 L5,-6 L18,-5 L8,4 L11,17 L0,10 L-11,17 L-8,4 L-18,-5 L-5,-6 Z");
    private static readonly Pen Outline = FrozenPen(Brushes.SaddleBrown, 3);
    private static Geometry FrozenGeometry(string path) { var value = Geometry.Parse(path); value.Freeze(); return value; }
    private static Pen FrozenPen(Brush brush, double width) { var value = new Pen(brush, width); value.Freeze(); return value; }
    internal bool DrawMotionProps { get; set; } = true;
    public MotionSurface()
    {
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        DataContextChanged += (_, _) => Attach(); Loaded += (_, _) => Attach(); Unloaded += (_, _) => Detach();
    }
    private void Attach()
    {
        Detach(); model = DataContext as CharacterViewModel;
        if (model != null) model.PropertyChanged += Changed;
        InvalidateVisual();
    }
    private void Detach() { if (model != null) model.PropertyChanged -= Changed; model = null; }
    private void Changed(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();
    internal static Transform AttachmentTransform(CharacterViewModel character, MotionAttachment attachment, double follow)
    {
        var pose = character.Motion; var rig = character.Rig;
        if (attachment is MotionAttachment.Head or MotionAttachment.Mouth)
        {
            var group = new TransformGroup();
            if (attachment == MotionAttachment.Mouth) group.Children.Add(new TranslateTransform(0, pose.MouthY * follow));
            group.Children.Add(new RotateTransform(pose.HeadAngle * follow, 256, rig.HeadCutY)); return group;
        }
        if (attachment is MotionAttachment.LeftHand or MotionAttachment.RightHand)
        {
            var limb = attachment == MotionAttachment.LeftHand ? rig.Left : rig.Right;
            if (!limb.Enabled) return Transform.Identity;
            var wrist = new MotionPoint(limb.PivotX, limb.Y + limb.Height * .9);
            var target = ConnectedMotion.Map(wrist, rig, pose, follow, false);
            var group = new TransformGroup(); double scale = 1 + (pose.HandScale - 1) * follow;
            group.Children.Add(new ScaleTransform(scale, scale, wrist.X, wrist.Y));
            group.Children.Add(new RotateTransform((attachment == MotionAttachment.LeftHand ? pose.LeftAngle : pose.RightAngle) * follow, wrist.X, wrist.Y));
            group.Children.Add(new TranslateTransform(target.X - wrist.X, target.Y - wrist.Y)); return group;
        }
        return Transform.Identity;
    }
    private (Geometry Clip, Transform Transform)[] Mesh(bool includeHead, double follow)
    {
        if (cachedPose != model!.Motion || cachedRig != model.Rig) { meshes.Clear(); cachedPose = model.Motion; cachedRig = model.Rig; }
        if (meshes.TryGetValue((includeHead, follow), out var cached)) return cached;
        var columns = ConnectedMotion.Columns(model.Rig); var rows = ConnectedMotion.Rows(model.Rig, includeHead);
        var parts = new List<(Geometry Clip, Transform Transform)>();
        void Triangle(Point a, Point b, Point c)
        {
            Point Destination(Point value) { var mapped = ConnectedMotion.Map(new(value.X, value.Y), model.Rig, model.Motion, follow, includeHead); return new(mapped.X, mapped.Y); }
            var da = Destination(a); var db = Destination(b); var dc = Destination(c);
            double x1 = b.X - a.X, y1 = b.Y - a.Y, x2 = c.X - a.X, y2 = c.Y - a.Y, det = x1 * y2 - y1 * x2;
            double m11 = ((db.X - da.X) * y2 - (dc.X - da.X) * y1) / det;
            double m21 = (x1 * (dc.X - da.X) - x2 * (db.X - da.X)) / det;
            double m12 = ((db.Y - da.Y) * y2 - (dc.Y - da.Y) * y1) / det;
            double m22 = (x1 * (dc.Y - da.Y) - x2 * (db.Y - da.Y)) / det;
            var transform = new MatrixTransform(new Matrix(m11, m12, m21, m22, da.X - m11 * a.X - m21 * a.Y, da.Y - m12 * a.X - m22 * a.Y)); transform.Freeze();
            var geometry = new StreamGeometry();
            using (var context = geometry.Open()) { context.BeginFigure(a, true, true); context.LineTo(b, true, false); context.LineTo(c, true, false); }
            geometry.Freeze(); parts.Add((geometry, transform));
        }
        for (int row = 0; row < rows.Length - 1; row++) for (int column = 0; column < columns.Length - 1; column++)
        {
            var a = new Point(columns[column], rows[row]); var b = new Point(columns[column + 1], rows[row]);
            var c = new Point(columns[column + 1], rows[row + 1]); var d = new Point(columns[column], rows[row + 1]);
            Triangle(a, b, c); Triangle(a, c, d);
        }
        return meshes[(includeHead, follow)] = parts.ToArray();
    }
    private void Deformed(DrawingContext drawing, ImageSource image, Rect rect, bool includeHead, double follow)
    {
        double cut = includeHead ? model!.Rig.HeadCutY - 24 : model!.Rig.HeadCutY;
        drawing.PushTransform(includeHead ? AttachmentTransform(model, MotionAttachment.Head, follow) : Transform.Identity);
        drawing.PushClip(new RectangleGeometry(new Rect(0, 0, 512, cut))); drawing.DrawImage(image, rect); drawing.Pop(); drawing.Pop();
        foreach (var part in Mesh(includeHead, follow))
        {
            drawing.PushTransform(part.Transform); drawing.PushClip(part.Clip); drawing.DrawImage(image, rect); drawing.Pop(); drawing.Pop();
        }
    }
    private void Sprite(DrawingContext drawing, ImageSource image, double opacity)
    {
        drawing.PushOpacity(opacity);
        if (model!.Motion.Gesture == null && model.Motion.HeadAngle == 0) drawing.DrawImage(image, CanvasRect);
        else Deformed(drawing, image, CanvasRect, model.Motion.HeadAngle != 0, 1);
        drawing.Pop();
    }
    private void Costumes(DrawingContext drawing, bool behind)
    {
        foreach (var item in model!.CostumeLayers.Where(item => item.Enabled && (item.Profile.CharacterId == null || item.Profile.CharacterId == model.CostumeCharacterId) && item.BehindCharacter == behind && item.Image != null))
        {
            var rect = new Rect(item.X, item.Y, item.DisplayWidth, item.DisplayHeight);
            var attachment = item.Profile.EffectiveAttachment;
            if (attachment == MotionAttachment.Garment && model.Motion.Gesture != null)
            {
                if (item.Profile.Follow == 0) drawing.DrawImage(item.Image, rect); else Deformed(drawing, item.Image!, rect, false, item.Profile.Follow);
            }
            else
            {
                drawing.PushTransform(AttachmentTransform(model, attachment, item.Profile.Follow));
                drawing.DrawImage(item.Image, rect); drawing.Pop();
            }
        }
    }
    private void DrawTear(DrawingContext drawing, double x, double y, double opacity)
    {
        if (opacity <= 0) return;
        drawing.PushOpacity(opacity); drawing.PushTransform(AttachmentTransform(model!, MotionAttachment.Head, 1));
        drawing.PushTransform(new TranslateTransform(x, y)); drawing.DrawGeometry(Brushes.LightSkyBlue, null, Tear);
        drawing.Pop(); drawing.Pop(); drawing.Pop();
    }
    private void Props(DrawingContext drawing)
    {
        var motion = model!.Motion; if (motion.Gesture == null || motion.Effect <= 0) return;
        var rig = model.Rig; var limb = rig.Right.Enabled ? rig.Right : rig.Left;
        if (!limb.Enabled) return;
        drawing.PushOpacity(motion.Effect);
        if (motion.Gesture == HandGesture.Heart)
        {
            drawing.PushTransform(new TranslateTransform(256 + motion.HandX, rig.HeadCutY + 60 + motion.HandY));
            drawing.DrawGeometry(Brushes.HotPink, Outline, Heart); drawing.Pop();
        }
        else
        {
            var wrist = ConnectedMotion.Map(new(limb.PivotX, limb.Y + limb.Height * .9), rig, motion);
            drawing.PushTransform(new TranslateTransform(wrist.X, wrist.Y));
            drawing.PushTransform(new RotateTransform(rig.Right.Enabled ? motion.RightAngle : motion.LeftAngle));
            switch (motion.Gesture)
            {
                case HandGesture.ToyGun:
                    drawing.DrawGeometry(Brushes.DeepSkyBlue, Outline, Gun);
                    drawing.DrawRoundedRectangle(Brushes.Orange, Outline, new Rect(57, -20, 13, 18), 4, 4);
                    drawing.DrawEllipse(Brushes.White, Outline, new Point(28, 6), 6, 7);
                    drawing.DrawRoundedRectangle(Brushes.Gold, Outline, new Rect(9, -31, 24, 13), 5, 5);
                    drawing.DrawEllipse(Brushes.LightSkyBlue, null, new Point(82 + motion.Effect * 12, -10), 5, 5); break;
                case HandGesture.ToySword:
                    drawing.DrawGeometry(Brushes.LightBlue, Outline, Sword);
                    drawing.DrawRectangle(Brushes.Gold, Outline, new Rect(-20, 0, 40, 8));
                    drawing.DrawRoundedRectangle(Brushes.Sienna, Outline, new Rect(-5, 8, 10, 23), 3, 3);
                    drawing.DrawLine(FrozenPen(Brushes.White, 2), new Point(0, -62), new Point(0, -8)); break;
                case HandGesture.MagicWand:
                    drawing.DrawRoundedRectangle(Brushes.MediumPurple, Outline, new Rect(-4, -68, 8, 80), 3, 3);
                    drawing.PushTransform(new TranslateTransform(0, -76)); drawing.DrawGeometry(Brushes.Gold, Outline, Star); drawing.Pop(); break;
                case HandGesture.HandRaise:
                    if (model.CostumeCharacterId == "pelly")
                    {
                        drawing.DrawEllipse(Brushes.FloralWhite, Outline, new Point(0, -10), 18, 28);
                        for (int i = 0; i < 3; i++) drawing.DrawEllipse(Brushes.FloralWhite, Outline, new Point(-12 + i * 12, -29), 7, 16);
                    }
                    else
                    {
                        var hand = model.CostumeCharacterId is "neulbo-man" or "neulbo-girl" ? Brushes.Bisque : Brushes.Tan;
                        drawing.DrawRoundedRectangle(hand, Outline, new Rect(-13, -24, 26, 30), 11, 11);
                        for (int i = 0; i < 4; i++) drawing.DrawRoundedRectangle(hand, Outline, new Rect(-12 + i * 7, -38 + Math.Abs(i - 1.5) * 3, 7, 25), 3, 3);
                        drawing.DrawRoundedRectangle(hand, Outline, new Rect(-20, -12, 13, 9), 4, 4);
                    }
                    break;
            }
            drawing.Pop(); drawing.Pop();
        }
        drawing.Pop();
    }
    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing); if (model == null) return;
        drawing.PushClip(new RectangleGeometry(CanvasRect));
        Costumes(drawing, true);
        if (model.PreviousSprite != null) Sprite(drawing, model.PreviousSprite, model.PreviousOpacity);
        Sprite(drawing, model.Sprite, model.Blend);
        DrawTear(drawing, model.TearLeftX, model.TearLeftY, model.TearLeftOpacity); DrawTear(drawing, model.TearRightX, model.TearRightY, model.TearRightOpacity);
        Costumes(drawing, false); if (DrawMotionProps) Props(drawing);
        if (model.ShowMotionGuides)
        {
            drawing.DrawLine(FrozenPen(Brushes.Gold, 2), new Point(0, model.Rig.HeadCutY), new Point(512, model.Rig.HeadCutY));
            foreach (var limb in new[] { model.Rig.Left, model.Rig.Right })
            {
                drawing.DrawRectangle(null, FrozenPen(limb.Enabled ? Brushes.Cyan : Brushes.Gray, 2), new Rect(limb.X, limb.Y, limb.Width, limb.Height));
                drawing.DrawEllipse(Brushes.Red, null, new Point(limb.PivotX, limb.PivotY), 4, 4);
            }
        }
        drawing.Pop();
    }
}
