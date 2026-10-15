using System.IO;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Actions;
using VoxPet.Core.Character;
using VoxPet.Core.Motion;
using VoxPet.Core.Packages;
using VoxPet.Core.Triggers;
using CharacterDefinition = VoxPet.Core.Character.CharacterDefinition;

namespace VoxPet.App.Views;

/// <summary>실제 ViewModel의 실패한 적용/지연 변환/미적용 입력 수명을 합성 패키지로 검사한다.</summary>
internal static class RuntimeSafetyQa
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-safety-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        var character = new CharacterViewModel();
        var native = new NativeCharacterViewModel(character, true, folder, () => 0);
        var editor = new CharacterEditorViewModel();
        try
        {
            byte[] png = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "ImageSetTemplate", "body.png"));
            VoxPetPackage Package(bool immediate)
            {
                var action = new ActionDefinition("overflow", "Overflow", 1000,
                    [new(immediate ? 0 : 100, ActionEventKind.Transform, "body", transform: new(scaleX: double.MaxValue))],
                    triggers: immediate ? [new("automatic", "overflow", TriggerKind.Interval, startImmediately: true)] : []);
                var definition = new CharacterDefinition([new("body", "body", "body.png", transform: new(scaleX: 2))],
                    actions: [action], motion: new MotionProfile(blinkEnabled: false, idleEnabled: false));
                return new VoxPetPackageValidator().Validate(new("qa.safety", "Synthetic Safety", "1", actions: ["actions/overflow.json"]), definition,
                    new Dictionary<string, byte[]> { ["body.png"] = png });
            }
            var valid = Package(false); var invalid = Package(true);
            string source = Path.Combine(folder, "valid.voxpet"), bad = Path.Combine(folder, "first-frame-error.voxpet");
            await Task.Run(() => { new VoxPetPackageExporter().Export(valid, source); new VoxPetPackageExporter().Export(invalid, bad); });
            check(await native.ImportAsync(source), "safety_native_valid_scene");
            var scene = native.Scene; var loaded = native.Loaded; var library = new CharacterPackageLibrary(Path.Combine(folder, "CharactersV2"));
            string? selection = library.Selected();
            check(!await native.ImportAsync(bad) && ReferenceEquals(scene, native.Scene) && ReferenceEquals(loaded, native.Loaded) && ReferenceEquals(scene, character.RuntimeScene), "safety_native_failed_first_frame_preserves_scene");
            check(library.Selected() == selection, "safety_native_failed_first_frame_preserves_selection");
            check(library.List().Count == 1, "safety_native_failed_import_does_not_enter_library");
            string brokenId = Guid.NewGuid().ToString("N"); File.Copy(bad, Path.Combine(folder, "CharactersV2", brokenId + ".voxpet"));
            check(!await native.SelectAsync(brokenId) && ReferenceEquals(scene, native.Scene) && ReferenceEquals(loaded, native.Loaded) && library.Selected() == selection, "safety_native_existing_bad_package_preserves_scene_selection");
            var main = new MainViewModel(true, () => [], _ => throw new InvalidOperationException(), dataFolder: folder, charactersFolder: Path.Combine(folder, "missing"), initialSettings: new(new()));
            var shell = new StudioShellViewModel(main, true);
            try
            {
                await shell.RefreshAsync();
                check(shell.Cards.Count(c => c.Legacy == null && !c.Sample) == 1 && shell.Status.StartsWith("일부 캐릭터"), "safety_library_bad_thumbnail_isolated");
            }
            finally { await shell.CloseAsync(); await main.DisposeAsync(); }
            for (int i = 0; i < 10000; i++) native.Input(new(TriggerKind.Manual, "unbound"));
            check(scene!.PendingInputCount == 128, "safety_runtime_pending_input_bounded");
            native.Update(0, 0); check(scene.PendingInputCount == 0, "safety_runtime_pending_input_drained");
            native.Play("overflow"); native.Update(.05, 0); var lastFrame = scene.Frame; int inputsChanged = 0; native.InputsChanged += () => inputsChanged++;
            native.Update(.2, 0);
            check(!native.CanReceiveInput && inputsChanged == 1 && native.Status.StartsWith("캐릭터 재생 중단") && ReferenceEquals(lastFrame, scene.Frame), "safety_native_delayed_overflow_holds_frame_disables_inputs");
            native.Input(new(TriggerKind.Manual, "overflow")); native.Play("overflow"); native.Update(.3, 1);
            check(scene.PendingInputCount == 0 && ReferenceEquals(lastFrame, scene.Frame), "safety_native_faulted_scene_ignores_further_ticks");
            check(await native.SelectAsync(loaded!.LibraryId) && native.CanReceiveInput, "safety_native_reselect_recovers_fault");

            check(await editor.OpenPackageAsync(valid), "safety_editor_valid_scene");
            var beforeCanvasEdit = editor.Document;
            editor.Fields.Single(f => f.Key == "width").Value = "1e-300"; await editor.ApplyAsync();
            check(ReferenceEquals(beforeCanvasEdit, editor.Document) && editor.Status.StartsWith("적용 실패") && !editor.CanExport, "safety_editor_extreme_canvas_preserves_document");
            editor.DiscardCommand.Execute(null);
            var document = editor.Document; var preview = editor.Scene;
            check(!await editor.OpenPackageAsync(invalid) && ReferenceEquals(document, editor.Document) && ReferenceEquals(preview, editor.Scene), "safety_editor_failed_first_frame_preserves_document_scene");
            editor.Page = 5; editor.PreviewAction = "overflow"; editor.Seek(0); editor.Seek(200);
            check(editor.Status.StartsWith("미리보기 재생 중단"), "safety_editor_scrub_overflow_handled");
            editor.StopCommand.Execute(null);
            await editor.DeleteAsync(); editor.Seek(0);
            check(editor.PreviewAction == null && editor.Document!.Package.Character.Actions.Count == 0, "safety_editor_deleted_action_clears_preview_selection");
            await editor.AddAsync(); editor.AddTriggerNext = false; await editor.AddDetailAsync();
            check(editor.Document!.Package.Character.Actions.Single().Timeline.Single().Target == "body", "safety_editor_anchorless_action_uses_part_target");
            editor.Page = 3; await editor.AddAsync();
            check(editor.Document.Package.Character.Mouth?.Anchor == "root" && editor.Document.Package.Character.Anchors.Single().Id == "root", "safety_editor_anchorless_mouth_creates_root");
            await editor.OpenPackageAsync(valid); editor.Page = 6; await editor.AddAsync();
            check(editor.Document.Package.Character.Attachments.Single().Layers.Single().Anchor == "root" && editor.Document.Package.Character.Anchors.Single().Id == "root", "safety_editor_anchorless_attachment_creates_root");
            await editor.NewAsync(); editor.Page = 2; editor.Selected = editor.Rows.Single(r => r.Id == "head");
            var field = editor.Fields.Single(f => f.Key == "x"); field.Value = "37"; document = editor.Document;
            await editor.MoveAnchorAsync("head", 200, 200);
            check(ReferenceEquals(document, editor.Document) && editor.Fields.Single(f => f.Key == "x").Value == "37" && !editor.CanExport, "safety_editor_drag_preserves_pending_form");
            await editor.DeleteAsync();
            check(ReferenceEquals(document, editor.Document) && editor.Fields.Single(f => f.Key == "x").Value == "37", "safety_editor_delete_preserves_pending_form");
            await editor.ApplyAsync(); check(editor.Document!.Package.Character.Anchors.Single(a => a.Id == "head").X == 37, "safety_editor_pending_form_still_applies");
            editor.Page = 4; editor.PreviewExpression = editor.Selected!.Id; await editor.DeleteAsync(); editor.ExpressionCommand.Execute(null);
            check(editor.PreviewExpression == editor.Expressions.FirstOrDefault()?.Id, "safety_editor_deleted_expression_reconciles_preview");
            editor.Page = 6; await editor.AddAsync(); editor.PreviewAttachment = editor.Selected!.Id; editor.AttachmentCommand.Execute(null); await editor.DeleteAsync(); editor.AttachmentCommand.Execute(null);
            check(editor.PreviewAttachment == null && editor.Attachments.Count == 0, "safety_editor_deleted_attachment_clears_preview_selection");
        }
        finally { await editor.CloseAsync(); await native.CloseAsync(); Directory.Delete(folder, true); }
    }
}
