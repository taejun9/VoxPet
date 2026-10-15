using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using VoxPet.App.ViewModels;

namespace VoxPet.App.Views;

/// <summary>동일 Frame renderer와 정적 Anchor overlay. drag를 Canvas 좌표로 변환해 edit transaction으로 전달한다.</summary>
public sealed class EditorCanvas : FrameworkElement
{
    private CharacterEditorViewModel? model;
    private string? dragging;
    private Point? target;
    public EditorCanvas()
    {
        DataContextChanged += (_, _) => Attach(); Loaded += (_, _) => Attach(); Unloaded += (_, _) => Detach();
        Focusable = true; Cursor = Cursors.Cross;
    }
    private void Attach() { Detach(); model = DataContext as CharacterEditorViewModel; if (model != null) model.PropertyChanged += Changed; InvalidateVisual(); }
    private void Detach() { if (model != null) model.PropertyChanged -= Changed; model = null; }
    private void Changed(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();
    private Rect Box
    {
        get
        {
            double width = model?.Document?.Package.Character.CanvasWidth ?? 512, height = model?.Document?.Package.Character.CanvasHeight ?? 512;
            double scale = Math.Min(ActualWidth / width, ActualHeight / height);
            return new((ActualWidth - width * scale) / 2, (ActualHeight - height * scale) / 2, width * scale, height * scale);
        }
    }
    private Point Pixel(double x, double y) => new(Box.X + x / model!.Document!.Package.Character.CanvasWidth * Box.Width, Box.Y + y / model.Document.Package.Character.CanvasHeight * Box.Height);
    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing); var box = Box;
        drawing.DrawRectangle(Brushes.WhiteSmoke, null, box);
        double stepX = box.Width / 16, stepY = box.Height / 16;
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) if ((x + y) % 2 == 0) drawing.DrawRectangle(Brushes.Gainsboro, null, new Rect(box.X + x * stepX, box.Y + y * stepY, stepX, stepY));
        if (model?.Scene == null) return;
        drawing.PushTransform(new TranslateTransform(box.X, box.Y));
        drawing.PushTransform(new MatrixTransform(RenderFrameDrawing.Matrix(VoxPet.Core.Character.AffineTransform2D.FromTransform(model.Scene.Frame.ViewportTransform))));
        RenderFrameDrawing.Draw(drawing, model.Scene.Frame, model.Scene.Images, width: box.Width, height: box.Height); drawing.Pop(); drawing.Pop();
        if (model.Page != 2 || !model.Guides || model.Document == null) return;
        var rig = model.Scene.Runtime.CurrentRig.Static;
        var labels = new List<Rect>();
        foreach (var anchor in model.Document.Package.Character.Anchors)
        {
            var position = rig[anchor.Id]; var p = Pixel(position.OffsetX, position.OffsetY);
            if (anchor.Parent != null) { var parent = rig[anchor.Parent]; drawing.DrawLine(new Pen(Brushes.LightSlateGray, 1), Pixel(parent.OffsetX, parent.OffsetY), p); }
            bool selected = model.Selected?.Id == anchor.Id; drawing.DrawEllipse(selected ? Brushes.Orange : Brushes.DodgerBlue, new Pen(Brushes.White, 1), p, selected ? 6 : 4, selected ? 6 : 4);
            var text = new FormattedText(anchor.Id, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, Brushes.DarkBlue, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            text.MaxTextWidth = Math.Max(40, Math.Min(120, ActualWidth - 12)); text.MaxTextHeight = 16; text.Trimming = TextTrimming.CharacterEllipsis;
            double labelX = Math.Clamp(p.X + 7, 0, Math.Max(0, ActualWidth - text.Width)), labelY = Math.Max(0, p.Y - 15);
            var label = new Rect(labelX, labelY, text.Width, 16);
            while (labels.Any(other => other.IntersectsWith(label))) label.Offset(0, 17);
            labels.Add(label); drawing.DrawText(text, label.TopLeft);
        }
        if (target is { } t) drawing.DrawEllipse(null, new Pen(Brushes.OrangeRed, 2), t, 8, 8);
    }
    internal Point AnchorScreenPoint(string id)
    {
        var p = model!.Scene!.Runtime.CurrentRig.Static[id]; return PointToScreen(Pixel(p.OffsetX, p.OffsetY));
    }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e); if (model?.Page != 2 || model.Scene == null || !model.CanEdit) return;
        var point = e.GetPosition(this); var nearest = model.Scene.Runtime.CurrentRig.Static.Select(p => (p.Key, Point: Pixel(p.Value.OffsetX, p.Value.OffsetY))).OrderBy(p => (p.Point - point).Length).FirstOrDefault();
        if (nearest.Key == null || (nearest.Point - point).Length > 15) return; dragging = nearest.Key; model.Selected = model.Rows.FirstOrDefault(r => r.Id == dragging); target = point; CaptureMouse(); e.Handled = true;
    }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (dragging != null) { target = e.GetPosition(this); InvalidateVisual(); } }
    protected override async void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e); if (dragging == null || model?.Document == null) return;
        string id = dragging; var p = e.GetPosition(this); dragging = null; target = null; ReleaseMouseCapture();
        await model.MoveAnchorAsync(id, (p.X - Box.X) / Box.Width * model.Document.Package.Character.CanvasWidth, (p.Y - Box.Y) / Box.Height * model.Document.Package.Character.CanvasHeight); e.Handled = true;
    }
}
