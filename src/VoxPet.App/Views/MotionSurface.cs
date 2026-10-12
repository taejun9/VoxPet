using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;

namespace VoxPet.App.Views;

/// <summary>원본 픽셀을 비파괴로 나눠 렌더한다. 의상 소매도 캐릭터와 동일한 영역/관절 변환을 사용한다.</summary>
public sealed class MotionSurface : FrameworkElement
{
    private CharacterViewModel? model;
    private CharacterRig? cachedRig;
    private Geometry head = Geometry.Empty, left = Geometry.Empty, right = Geometry.Empty, torso = Geometry.Empty, garment = Geometry.Empty;
    private static readonly Rect CanvasRect = new(0, 0, 512, 512);
    private static readonly Geometry Tear = FrozenGeometry("M5,0 C4,5 0,8 0,12 C0,20 10,20 10,12 C10,8 6,5 5,0 Z");
    private static readonly Geometry Heart = FrozenGeometry("M0,8 C-32,-14 -29,-36 -12,-35 C-4,-35 0,-29 0,-25 C0,-29 4,-35 12,-35 C29,-36 32,-14 0,8 Z");
    private static readonly Geometry Gun = FrozenGeometry("M0,0 L0,-18 L65,-18 L65,-4 L28,-4 L22,20 L8,20 L13,0 Z");
    private static readonly Geometry Sword = FrozenGeometry("M-5,0 L-5,-63 L0,-78 L5,-63 L5,0 Z M-18,0 L18,0 L18,7 L-18,7 Z M-4,7 L4,7 L4,28 L-4,28 Z");
    private static readonly Geometry Star = FrozenGeometry("M0,-18 L5,-6 L18,-5 L8,4 L11,17 L0,10 L-11,17 L-8,4 L-18,-5 L-5,-6 Z");
    private static readonly Pen Outline = FrozenPen(Brushes.SaddleBrown, 3);
    private static Geometry FrozenGeometry(string path) { var value = Geometry.Parse(path); value.Freeze(); return value; }
    private static Pen FrozenPen(Brush brush, double width) { var value = new Pen(brush, width); value.Freeze(); return value; }
    public MotionSurface()
    {
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
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
    private static Geometry Region(LimbRegion limb) => limb.Enabled ? new RectangleGeometry(new Rect(limb.X, limb.Y, limb.Width, limb.Height)) : Geometry.Empty;
    private void Cache(CharacterRig rig)
    {
        if (cachedRig == rig) return;
        cachedRig = rig; head = new RectangleGeometry(new Rect(0, 0, 512, rig.HeadCutY)); left = Region(rig.Left); right = Region(rig.Right);
        var arms = new CombinedGeometry(GeometryCombineMode.Union, left, right);
        garment = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(CanvasRect), arms);
        torso = new CombinedGeometry(GeometryCombineMode.Exclude, garment, head);
        foreach (var geometry in new[] { head, left, right, torso, garment }) if (geometry.CanFreeze) geometry.Freeze();
    }
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
            var group = new TransformGroup(); double scale = 1 + (pose.HandScale - 1) * follow;
            group.Children.Add(new ScaleTransform(scale, scale, limb.PivotX, limb.PivotY));
            group.Children.Add(new RotateTransform((attachment == MotionAttachment.LeftHand ? pose.LeftAngle : pose.RightAngle) * follow, limb.PivotX, limb.PivotY));
            group.Children.Add(new TranslateTransform(pose.HandX * follow, pose.HandY * follow)); return group;
        }
        return Transform.Identity;
    }
    private void Part(DrawingContext drawing, ImageSource image, Rect rect, Geometry clip, MotionAttachment attachment, double follow)
    {
        bool hand = attachment is MotionAttachment.LeftHand or MotionAttachment.RightHand;
        if (hand) drawing.PushClip(new RectangleGeometry(new Rect(0, model!.Rig.HeadCutY, 512, 512 - model.Rig.HeadCutY)));
        drawing.PushTransform(AttachmentTransform(model!, attachment, follow)); drawing.PushClip(clip);
        drawing.DrawImage(image, rect); drawing.Pop(); drawing.Pop(); if (hand) drawing.Pop();
    }
    private void Sprite(DrawingContext drawing, ImageSource image, double opacity)
    {
        drawing.PushOpacity(opacity);
        if (model!.Motion.Gesture == null && model.Motion.HeadAngle == 0) drawing.DrawImage(image, CanvasRect);
        else
        {
            Part(drawing, image, CanvasRect, torso, MotionAttachment.Body, 1);
            Part(drawing, image, CanvasRect, head, MotionAttachment.Head, 1);
            Part(drawing, image, CanvasRect, left, MotionAttachment.LeftHand, 1);
            Part(drawing, image, CanvasRect, right, MotionAttachment.RightHand, 1);
        }
        drawing.Pop();
    }
    private void Costumes(DrawingContext drawing, bool behind)
    {
        foreach (var item in model!.CostumeLayers.Where(item => item.Enabled && item.BehindCharacter == behind && item.Image != null))
        {
            var rect = new Rect(item.X, item.Y, item.DisplayWidth, item.DisplayHeight);
            var attachment = item.Profile.EffectiveAttachment;
            if (attachment == MotionAttachment.Garment && model.Motion.Gesture != null)
            {
                Part(drawing, item.Image!, rect, garment, MotionAttachment.Body, item.Profile.Follow);
                Part(drawing, item.Image!, rect, left, MotionAttachment.LeftHand, item.Profile.Follow);
                Part(drawing, item.Image!, rect, right, MotionAttachment.RightHand, item.Profile.Follow);
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
            drawing.PushTransform(AttachmentTransform(model, rig.Right.Enabled ? MotionAttachment.RightHand : MotionAttachment.LeftHand, 1));
            drawing.PushTransform(new TranslateTransform(limb.PivotX, limb.Y + limb.Height - 16));
            switch (motion.Gesture)
            {
                case HandGesture.ToyGun:
                    drawing.DrawGeometry(Brushes.DeepSkyBlue, Outline, Gun); drawing.DrawRectangle(Brushes.Orange, null, new Rect(60, -18, 8, 14)); break;
                case HandGesture.ToySword: drawing.DrawGeometry(Brushes.LightBlue, Outline, Sword); break;
                case HandGesture.MagicWand:
                    drawing.DrawRoundedRectangle(Brushes.MediumPurple, Outline, new Rect(-4, -68, 8, 80), 3, 3);
                    drawing.PushTransform(new TranslateTransform(0, -76)); drawing.DrawGeometry(Brushes.Gold, Outline, Star); drawing.Pop(); break;
                case HandGesture.Claw:
                    for (int i = 0; i < 3; i++) drawing.DrawLine(FrozenPen(Brushes.LightSalmon, 4), new Point(i * 12 - 12, -40), new Point(i * 12 - 25, -8)); break;
            }
            drawing.Pop(); drawing.Pop();
        }
        drawing.Pop();
    }
    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing); if (model == null) return; Cache(model.Rig);
        drawing.PushClip(new RectangleGeometry(CanvasRect));
        Costumes(drawing, true);
        if (model.PreviousSprite != null) Sprite(drawing, model.PreviousSprite, model.PreviousOpacity);
        Sprite(drawing, model.Sprite, model.Blend);
        DrawTear(drawing, model.TearLeftX, model.TearLeftY, model.TearLeftOpacity); DrawTear(drawing, model.TearRightX, model.TearRightY, model.TearRightOpacity);
        Costumes(drawing, false); Props(drawing);
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
