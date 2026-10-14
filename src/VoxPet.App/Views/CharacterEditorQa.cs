using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using VoxPet.Core.Editing;
using VoxPet.Core.Motion;
using System.Windows;
using System.Windows.Media;
using VoxPet.App.ViewModels;
using VoxPet.Core.Actions;
using VoxPet.Core.Character;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;

namespace VoxPet.App.Views;

/// <summary>공개 제작 틀과 실제 WPF editor controls/transaction/desktop drag/export를 검사한다.</summary>
internal static class CharacterEditorQa
{
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);
    private static T? Find<T>(DependencyObject parent) where T : DependencyObject
    { if (parent is T t) return t; for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) if (Find<T>(VisualTreeHelper.GetChild(parent, i)) is { } value) return value; return null; }
    private static void Set(CharacterEditorViewModel editor, string key, string value, bool detail = false) => (detail ? editor.DetailFields : editor.Fields).Single(f => f.Key == key).Value = value;
    public static async Task RunAsync(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-editor-qa-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        var mainModel = new MainViewModel(true, () => [], _ => throw new InvalidOperationException(), dataFolder: folder, charactersFolder: Path.Combine(folder, "missing"), initialSettings: new(new()));
        var window = new MainWindow(true, new Rect(0, 0, 1400, 900), mainModel); var editor = window.Studio.Editor;
        try
        {
            window.Show(); await window.Ready;
            var workArea = SystemParameters.WorkArea;
            window.Width = Math.Min(1280, workArea.Width - 48); window.Height = Math.Min(820, workArea.Height - 48);
            window.Left = workArea.Left + 24; window.Top = workArea.Top + 24; window.ShowStudio();
            await ScreenEvidence.CaptureAsync(window, "editor-home", check, new { wireframe = 1 });
            await editor.NewAsync(); check(editor.HasDocument && editor.Scene != null, "editor_new_character_common_runtime"); window.ShowStudio(5);
            Set(editor, "name", "QA Editor"); Set(editor, "author", "VoxPet QA"); await editor.ApplyAsync();
            check(editor.Document!.Package.Manifest.Name == "QA Editor" && editor.Document.Package.Manifest.Author == "VoxPet QA", "editor_metadata_apply");
            await ScreenEvidence.CaptureAsync(window, "editor-basic", check, new { wireframe = 3 });
            editor.Page = 1; editor.Selected = editor.Rows.Single(r => r.Id == "head"); Set(editor, "z", "220"); Set(editor, "visible", "False"); await editor.ApplyAsync();
            check(!editor.Document.Package.Character.Parts.Single(p => p.Id == "head").Visible && !editor.Scene!.Frame.Layers.Any(l => l.Id == "part-head"), "editor_part_visibility_runtime");
            Set(editor, "visible", "True"); await editor.ApplyAsync();
            Set(editor, "z", "225"); editor.Selected = editor.Rows.First(); editor.Selected = editor.Rows.Single(r => r.Id == "head");
            check(editor.Fields.Single(f => f.Key == "z").Value == "225" && !editor.CanExport, "editor_pending_selection_preserved_blocks_export");
            var pendingDocument = editor.Document; await editor.AddAsync(); check(ReferenceEquals(pendingDocument, editor.Document) && editor.Fields.Single(f => f.Key == "z").Value == "225", "editor_pending_structural_edit_preserved"); await editor.ApplyAsync();
            await ScreenEvidence.CaptureAsync(window, "editor-parts", check, new { wireframe = 4 });
            editor.Page = 2; editor.Selected = editor.Rows.Single(r => r.Id == "head"); var old = editor.Document.Package.Character.Anchors.Single(a => a.Id == "head");
            var world = editor.Scene!.Runtime.CurrentRig.Static["head"].Map(0, 0); await editor.MoveAnchorAsync("head", world.X + 25, world.Y + 15);
            var moved = editor.Scene.Runtime.CurrentRig.Static["head"].Map(0, 0); check(Math.Abs(moved.X - world.X - 25) < .001 && Math.Abs(moved.Y - world.Y - 15) < .001, "editor_anchor_parent_local_transaction");
            editor.Selected = editor.Rows.Single(r => r.Id == "root"); Set(editor, "parent", "head"); await editor.ApplyAsync(); check(editor.Status.StartsWith("적용 실패") && !editor.CanExport, "editor_parent_cycle_preserves_valid_preview"); editor.DiscardCommand.Execute(null);
            await ScreenEvidence.CaptureAsync(window, "editor-anchors", check, new { wireframe = 5 });
            window.UpdateLayout(); var canvas = Find<EditorCanvas>(window)!; var point = canvas.AnchorScreenPoint("head");
            SetCursorPos((int)point.X, (int)point.Y); mouse_event(2, 0, 0, 0, UIntPtr.Zero); await Task.Delay(30); SetCursorPos((int)point.X + 16, (int)point.Y + 10); await Task.Delay(30); mouse_event(4, 0, 0, 0, UIntPtr.Zero);
            for (int i = 0; i < 40 && editor.IsBusy; i++) await Task.Delay(25); await Task.Delay(100);
            var dragged = editor.Scene!.Runtime.CurrentRig.Static["head"].Map(0, 0); check((new Vector(dragged.X - moved.X, dragged.Y - moved.Y)).Length > 1, "editor_real_mouse_anchor_drag");
            editor.Page = 3; while (editor.Document.Package.Character.Mouth!.Frames.Count < 16) await editor.AddAsync();
            check(editor.Document.Package.Character.Mouth.Frames.Count == 16, "editor_mouth_16_frames"); editor.Voice = 1; check(editor.Scene!.Runtime.MouthFrame == 15, "editor_mouth_slider_same_runtime");
            await ScreenEvidence.CaptureAsync(window, "editor-face", check, new { wireframe = 6 });
            string png = Path.Combine(AppContext.BaseDirectory, "Assets", "ImageSetTemplate", "head.png"); Set(editor, "bodyBounce", "7");
            check(await editor.ReplaceImageAsync(png, "asset") && editor.Fields.Single(f => f.Key == "bodyBounce").Value == "7", "editor_png_prepare_keeps_pending_form"); await editor.ApplyAsync();
            string invalid = Path.Combine(folder, "bad.png"); File.WriteAllText(invalid, "bad"); var before = editor.Document;
            check(!await editor.ReplaceImageAsync(invalid, "asset") && ReferenceEquals(before, editor.Document), "editor_bad_png_preserves_document");
            editor.Page = 4; Set(editor, "name", "QA Neutral"); await editor.ApplyAsync(); check(editor.Document.Package.Character.Expressions.First().Name == "QA Neutral", "editor_expression_edit");
            await ScreenEvidence.CaptureAsync(window, "editor-expressions", check, new { wireframe = 7 });
            editor.Page = 5; await editor.AddAsync(); editor.Selected = editor.Rows.Last(); Set(editor, "name", "QA Action"); Set(editor, "priority", "80"); await editor.ApplyAsync(); await editor.AddDetailAsync();
            Set(editor, "target", "head", true); Set(editor, "x", "30", true); await editor.ApplyDetailAsync();
            editor.AddTriggerNext = true; await editor.AddDetailAsync(); editor.SelectedDetail = editor.Details.Last(r => r.Item is VoxPet.Core.Triggers.TriggerDefinition); Set(editor, "kind", "VoiceThreshold", true); Set(editor, "threshold", "0.5", true); await editor.ApplyDetailAsync();
            editor.Seek(500); check(editor.Scene!.Runtime.State.ActionId == editor.Document.Package.Character.Actions.Last().Id && Math.Abs(editor.Scene.Frame.Layers.Single(l => l.Id == "part-head").X - 30) < .001, "editor_action_timeline_scrub_frame");
            await editor.AddPoseAsync(); Set(editor, "sprite:head", "숨김", true); await editor.ApplyDetailAsync(); check(editor.Document.Package.Character.Poses.Last().Sprites["head"] == null, "editor_pose_override_edit");
            await ScreenEvidence.CaptureAsync(window, "editor-actions", check, new { wireframe = 8 });
            editor.Page = 6; await editor.AddAsync(); editor.Selected = editor.Rows.Last(); Set(editor, "category", "faceDecoration"); Set(editor, "name", "QA Attachment"); await editor.ApplyAsync();
            Set(editor, "anchor", "mouth", true); Set(editor, "mouthMotion", "True", true); Set(editor, "x", "15", true); Set(editor, "y", "20", true); Set(editor, "width", "50", true); Set(editor, "height", "60", true); Set(editor, "pose:idle", editor.Document.Package.AssetPaths.First(), true); await editor.ApplyDetailAsync();
            string att = editor.Document.Package.Character.Attachments.Last().Id; editor.PreviewAttachment = att; editor.AttachmentCommand.Execute(null); check(editor.Scene!.Runtime.Attachments.EnabledIds.Contains(att) && editor.Scene.Frame.Layers.Any(l => l.Id.StartsWith("attachment-")), "editor_attachment_same_runtime_preview");
            await ScreenEvidence.CaptureAsync(window, "editor-attachments", check, new { wireframe = 9 });
            editor.Page = 7; editor.PausePreview(); editor.Seek(0); await editor.ValidateAsync(); check(editor.Status.StartsWith("검증 통과"), "editor_validate_package_reload"); string export = Path.Combine(folder, "edited.voxpet"); check(await editor.ExportAsync(export), "editor_export_atomic_voxpet");
            await ScreenEvidence.CaptureAsync(window, "editor-export", check, new { wireframe = 10 });
            var decoded = await Task.Run(() => new VoxPetPackageLoader().Load(export)); var original = editor.Document.Package;
            check(decoded.Manifest.Name == original.Manifest.Name && decoded.Character.Mouth!.Frames.Count == 16 && decoded.Character.Poses.Count == original.Character.Poses.Count && decoded.Character.Attachments.Count == 1, "editor_export_reload_supported_models");
            check(original.AssetPaths.All(p => original.ReadAsset(p).SequenceEqual(decoded.ReadAsset(p))), "editor_export_reload_original_png_bytes");
            check(new CharacterDocument(decoded).Export().SequenceEqual(editor.Document.Export()), "editor_export_reload_all_supported_fields_exact");
            var exportedRuntime = decoded.CreateRuntime(new Random(1));
            foreach (string id in editor.Scene!.Runtime.Attachments.EnabledIds) exportedRuntime.Attachments.SetEnabled(id, true);
            var exportedFrame = exportedRuntime.Update(new(0, 0, editor.Voice));
            check(JsonSerializer.Serialize(exportedFrame) == JsonSerializer.Serialize(editor.Scene.Frame), "editor_export_reload_runtime_frame_exact");
            mainModel.PauseVisualForQa();
            check(await mainModel.Packages.ImportAsync(export), "editor_export_select_broadcast_package"); await window.Studio.RefreshAsync(); window.ShowStudio(1); window.Studio.Selected = window.Studio.Cards.Single(c => c.Legacy == null);
            await ScreenEvidence.CaptureAsync(window, "editor-library", check, new { wireframe = 2 });
            var preview = decoded.CreateRuntime(new Random(1)); preview.Update(new(0, 0, 0)); mainModel.Packages.Update(mainModel.Packages.Scene!.Runtime.Actions.LastTime, 0);
            check(mainModel.Packages.Scene.Runtime.Definition.Mouth!.Frames.Count == editor.Scene!.Runtime.Definition.Mouth!.Frames.Count, "editor_broadcast_uses_exported_definition");
            window.ShowStudio(5); check(await editor.OpenAsync(export), "editor_import_exported_package"); check(editor.Document.Package.Manifest.Name == "QA Editor", "editor_reimport_metadata_preserved");
            var capture = editor.Document; string bad = Path.Combine(folder, "bad.voxpet"); File.WriteAllText(bad, "bad"); check(!await editor.OpenAsync(bad) && ReferenceEquals(capture, editor.Document), "editor_failed_import_keeps_document");
            // Full-canvas imports expose every expression bank and preserve all untouched PNG frames.
            string asset = original.AssetPaths.First(); string alternate = original.AssetPaths.Last();
            var sheet = new VoxPet.Core.Character.CharacterDefinition([new CharacterPart("sprite", "sprite", asset, "root")], [new AnchorDefinition("root")], 768, 384,
                expressions: [new("neutral", asset, asset), new("happy", alternate, alternate)],
                spriteSheet: new("sprite", [new("neutral", [asset, asset, asset], [asset, asset, asset]), new("happy", [alternate, alternate, alternate], [alternate, alternate, alternate])]),
                motion: new MotionProfile(blinkEnabled: false, idleEnabled: false, bodyAnchor: "root", headAnchor: "root"));
            var sheetPackage = new VoxPetPackageValidator().Validate(new("qa.editor.sheet", "Sheet", "1.0"), sheet, original.AssetPaths.ToDictionary(p => p, original.ReadAsset));
            check(await editor.OpenPackageAsync(sheetPackage), "editor_full_sprite_import"); editor.PausePreview(); editor.Page = 3; editor.SpriteBank = "happy"; Set(editor, "closed", asset); Set(editor, "bankBlink", "False"); await editor.ApplyAsync();
            check(editor.Document!.Package.Character.SpriteSheet!.Expressions[0].Closed[0] == asset && editor.Document.Package.Character.SpriteSheet.Expressions[1].Closed[0] == asset && !editor.Document.Package.Character.SpriteSheet.Expressions[1].Blink, "editor_full_sprite_selected_bank_edit");
            editor.Page = 4; await editor.AddAsync(); editor.Selected = editor.Rows.Last(); string addedExpression = editor.Selected.Id;
            Set(editor, "id", "qa-renamed"); await editor.ApplyAsync(); check(editor.Document.Package.Character.SpriteSheet.Expressions.Any(e => e.Id == "qa-renamed"), "editor_full_sprite_expression_add_rename");
            editor.Selected = editor.Rows.Single(r => r.Id == "qa-renamed"); await editor.DeleteAsync(); check(editor.Document.Package.Character.SpriteSheet.Expressions.Count == 2, "editor_full_sprite_expression_delete");
            editor.Page = 3; await editor.AddAsync(); check(editor.Document.Package.Character.SpriteSheet.Selector.Frames.Count == 4 && editor.Document.Package.Character.SpriteSheet.Expressions.All(e => e.Open.Count == 4), "editor_full_sprite_all_banks_frame_count");
            check(editor.Document.Package.Character.CanvasWidth == 768 && editor.Document.Package.Character.CanvasHeight == 384, "editor_non_square_canvas_preserved");
            editor.Page = 7; await editor.ValidateAsync(); check(editor.Status.StartsWith("검증 통과"), "editor_full_sprite_validate_export");
        }
        finally { await window.ShutdownAsync(); window.Close(); Directory.Delete(folder, true); }
    }
}
