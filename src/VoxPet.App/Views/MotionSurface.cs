using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using VoxPet.App.ViewModels;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;

namespace VoxPet.App.Views;

/// <summary>공유 모델의 최종 Core frame만 표시한다. 자산/표정/의상/입/모션 판단은 Runtime에 있다.</summary>
public sealed class MotionSurface : FrameworkElement
{
    private CharacterViewModel? model;
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
    // 기존 QA/API 호환. 계산은 UI 독립 Core provider에 위임한다.
    internal static Transform AttachmentTransform(CharacterViewModel character, MotionAttachment attachment, double follow)
        => new MatrixTransform(RenderFrameDrawing.Matrix(LegacyRenderGeometryProvider.Attachment(character.Rig, character.Motion, attachment, follow)));
    protected override void OnRender(DrawingContext drawing)
    {
        base.OnRender(drawing);
        if (model?.RuntimeScene is { } scene) RenderFrameDrawing.Draw(drawing, scene.Frame, scene.Images, DrawMotionProps);
    }
}
