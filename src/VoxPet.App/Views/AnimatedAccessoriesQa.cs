using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VoxPet.App.ViewModels;
using VoxPet.Core.Packages;

namespace VoxPet.App.Views;

internal static class AnimatedAccessoriesQa
{
    private static IEnumerable<T> Find<T>(DependencyObject node) where T : DependencyObject
    { if (node is T item) yield return item; for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Find<T>(VisualTreeHelper.GetChild(node, i))) yield return child; }
    public static async Task<double> RunAsync(MainWindow window, MainViewModel model, string folder, double now, Action<bool, string> check)
    {
        var studio = window.Studio; var scene = model.Packages.Scene!;
        studio.AttachmentCards.Single(c => c.Id == "hat").ToggleCommand.Execute(null);
        studio.AttachmentCards.Single(c => c.Id == "cape").ToggleCommand.Execute(null);
        foreach (string hair in new[] { "hair-short", "hair-long", "hair-afro" })
        {
            studio.AttachmentCards.Single(c => c.Id == hair).ToggleCommand.Execute(null); model.Packages.Update(now += .16, 0);
            check(scene.Runtime.Attachments.EnabledIds.Count(id => id.StartsWith("hair-")) == 1 && scene.Runtime.Attachments.EnabledIds.Contains(hair) && scene.Runtime.Attachments.EnabledIds.Contains("hat") && scene.Runtime.Attachments.EnabledIds.Contains("cape"), "studio_hair_with_hat_and_wings_" + hair);
            await ScreenEvidence.CaptureAsync(window, "studio-hair-" + hair, check);
        }
        var broadcast = window.ShowCharacter(); var frames = new HashSet<string>();
        try
        {
            for (int i = 0; i < 4; i++)
            {
                model.Packages.Update(now += .16, 0);
                check(scene.Runtime.State.ActionId == null && new[] { "hat", "cape" }.All(id => scene.Runtime.Definition.Attachments.Single(a => a.Id == id).Layers.All(l => scene.Frame.Layers.Any(r => r.Asset == l.Animation!.FrameAt(now)))), "studio_accessory_idle_frame_" + i);
                frames.Add(UnifiedStudioQa.Render(model.Character));
                check(ReferenceEquals(model.Character.RuntimeScene, scene) && ReferenceEquals(broadcast.DataContext, model), "studio_accessory_broadcast_shared_" + i);
                await ScreenEvidence.CaptureAsync(window, "studio-accessory-animation-" + i, check);
                await ScreenEvidence.CaptureAsync(broadcast, "studio-accessory-broadcast-" + i, check);
            }
            check(frames.Count == 4, "studio_accessory_idle_four_distinct_frames");
        }
        finally { broadcast.Close(); }
        studio.RemoveAttachmentsCommand.Execute(null); model.Packages.Update(now += .1, 0);
        check(!scene.Frame.Layers.Any(l => l.Asset.StartsWith("samples/hat") || l.Asset.StartsWith("samples/cape") || l.Asset.StartsWith("samples/hair-")), "studio_animated_accessories_remove");
        await CheckEditorAsync(window, folder, check);
        window.ShowStudio(2);
        return now;
    }
    private static async Task CheckEditorAsync(MainWindow window, string folder, Action<bool, string> check)
    {
        var original = new VoxPetPackageLoader().Load(StudioShellViewModel.SamplePath);
        var editor = window.Studio.Editor; check(await editor.OpenPackageAsync(original), "studio_animation_editor_open");
        window.ShowStudio(5); editor.PausePreview(); editor.Page = 6; editor.Selected = editor.Rows.Single(r => r.Id == "hat");
        foreach (var pair in new[] { ("expression", (object)editor.Expressions), ("action", editor.Actions), ("attachment", editor.Attachments) })
        {
            window.UpdateLayout(); var combo = Find<ComboBox>(window.StudioSurface).Single(c => ReferenceEquals(c.ItemsSource, pair.Item2));
            combo.BringIntoView(); await UnifiedStudioQa.CheckSelectorAsync(combo, "editor_" + pair.Item1, check);
        }
        for (DependencyObject? ancestor = Find<CharacterEditorView>(window.StudioSurface).Single(); ancestor != null; ancestor = VisualTreeHelper.GetParent(ancestor)) if (ancestor is ScrollViewer scroll) scroll.ScrollToTop();
        void Set(string key, string value) => editor.DetailFields.Single(f => f.Key == key).Value = value;
        Set("animationMs", "160"); Set("animation:0", "samples/hat-1.png"); await editor.ApplyDetailAsync();
        var hat = editor.Document!.Package.Character.Attachments.Single(a => a.Id == "hat").Layers[0];
        check(hat.Animation is { FrameDurationMs: 160 } && hat.Animation.Frames.Count == 8 && hat.Animation.Frames[0] == "samples/hat-1.png", "studio_animation_editor_speed_and_frame");
        Set("z", (hat.ZIndex + 1).ToString()); await editor.ApplyDetailAsync();
        check(editor.Document.Package.Character.Attachments.Single(a => a.Id == "hat").Layers[0].Animation!.Frames.SequenceEqual(hat.Animation!.Frames), "studio_animation_editor_unrelated_edit_preserves_frames");
        var before = editor.Document; Set("animationCount", "1"); await editor.ApplyDetailAsync();
        check(ReferenceEquals(before, editor.Document) && editor.Status.StartsWith("적용 실패") && !editor.CanExport, "studio_animation_editor_invalid_count_rollback"); editor.DiscardCommand.Execute(null);
        Set("animationCount", "0"); await editor.ApplyDetailAsync();
        check(editor.Document.Package.Character.Attachments.Single(a => a.Id == "hat").Layers[0].Animation == null, "studio_animation_editor_static_option");
        Set("animationCount", "2"); await editor.ApplyDetailAsync();
        check(editor.DetailFields.Count(f => f.Key.StartsWith("animation:")) == 2, "studio_animation_editor_grows_frame_fields");
        Set("animationMs", "160"); Set("animation:0", "samples/hat.png"); Set("animation:1", "samples/hat-2.png"); await editor.ApplyDetailAsync();
        editor.PreviewAttachment = "hat"; editor.AttachmentCommand.Execute(null); editor.Seek(160);
        check(editor.Scene!.Frame.Layers.Any(l => l.Asset == "samples/hat-2.png"), "studio_animation_editor_preview_frame");
        await CaptureFieldsAsync(window, editor, "hat", check);
        editor.Selected = editor.Rows.Single(r => r.Id == "cape"); Set("z", "-11"); await editor.ApplyDetailAsync();
        check(editor.Document.Package.Character.Attachments.Single(a => a.Id == "cape").Layers[0].Animation is { FrameDurationMs: 140, Frames.Count: 6 }, "studio_animation_editor_wings_preserved");
        editor.PreviewAttachment = "cape"; editor.AttachmentCommand.Execute(null); editor.Seek(280);
        check(editor.Scene!.Frame.Layers.Any(l => l.Asset == "samples/cape-2.png"), "studio_animation_editor_wings_preview");
        await CaptureFieldsAsync(window, editor, "cape", check);
        string exported = Path.Combine(folder, "animated-edited.voxpet");
        check(await editor.ExportAsync(exported), "studio_animation_editor_export");
        var loaded = new VoxPetPackageLoader().Load(exported); var changed = loaded.Character.Attachments.Single(a => a.Id == "hat").Layers[0].Animation!;
        check(changed.Frames.SequenceEqual(new[] { "samples/hat.png", "samples/hat-2.png" }) && changed.FrameDurationMs == 160 && loaded.Character.Attachments.Single(a => a.Id == "cape").Layers[0].Animation!.Frames.Count == 6 && original.AssetPaths.All(p => original.ReadAsset(p).SequenceEqual(loaded.ReadAsset(p))), "studio_animation_editor_export_preserves_metadata_and_all_png");
    }
    private static async Task CaptureFieldsAsync(MainWindow window, CharacterEditorViewModel editor, string id, Action<bool, string> check)
    {
        window.UpdateLayout();
        var field = editor.DetailFields.Single(f => f.Key == "animationCount");
        var box = Find<TextBox>(window.StudioSurface).Single(t => ReferenceEquals(t.DataContext, field));
        for (DependencyObject? ancestor = box; ancestor != null; ancestor = VisualTreeHelper.GetParent(ancestor))
            if (ancestor is ScrollViewer { Content: UIElement content } scroll && content is not TextBox)
            { scroll.ScrollToVerticalOffset(box.TranslatePoint(new Point(), content).Y - 10); break; }
        await ScreenEvidence.CaptureAsync(window, "studio-editor-animation-" + id, check);
    }
}
