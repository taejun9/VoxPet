using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;

namespace VoxPet.App.Views;

/// <summary>두 카탈로그 선택에 직접 연결된 샘플. 공개 교정 도형만 사용하며 개인 시트는 읽지 않는다.</summary>
internal static class CompanionSamplesQa
{
    private static IEnumerable<T> Find<T>(DependencyObject node) where T : DependencyObject
    { if (node is T item) yield return item; for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Find<T>(VisualTreeHelper.GetChild(node, i))) yield return child; }
    private static async Task WaitAsync(Func<bool> done)
    { for (int i = 0; i < 250 && !done(); i++) await Task.Delay(20); if (!done()) throw new InvalidOperationException("Companion QA operation did not finish"); }
    private static bool ClearFace(ImageSource source, bool side)
    {
        var bitmap = new FormatConvertedBitmap((BitmapSource)source, PixelFormats.Bgra32, null, 0);
        foreach (var point in side ? new[] { new Point(252, 153), new Point(178, 203) } : new[] { new Point(194, 197), new Point(322, 197), new Point(256, 239) })
        {
            var pixels = new byte[12 * 12 * 4]; bitmap.CopyPixels(new Int32Rect((int)point.X - 6, (int)point.Y - 6, 12, 12), pixels, 48, 0);
            if (pixels.Where((_, index) => index % 4 == 3).Any(alpha => alpha > 32)) return false;
        }
        return true;
    }
    internal static void SaveCalibration(string path, bool side)
    {
        int columns = side ? 3 : 8; var sheet = new DrawingVisual();
        using (var drawing = sheet.RenderOpen())
            for (int eye = 0; eye < 2; eye++) for (int mouth = 0; mouth < columns; mouth++)
            {
                drawing.PushTransform(new TranslateTransform(mouth * 128, eye * 128)); drawing.PushTransform(new ScaleTransform(.25, .25));
                var outline = new Pen(Brushes.DarkSlateGray, 5);
                drawing.DrawEllipse(Brushes.LightSteelBlue, outline, side ? new Point(293, 334) : new Point(256, 381), side ? 105 : 130, side ? 91 : 104);
                drawing.DrawEllipse(Brushes.LightBlue, outline, side ? new Point(303, 165) : new Point(256, 178), side ? 100 : 165, side ? 92 : 120);
                if (side)
                {
                    drawing.DrawRoundedRectangle(Brushes.PaleGoldenrod, outline, new Rect(86, 153, 176, 76), 35, 35);
                    drawing.DrawEllipse(Brushes.CadetBlue, outline, new Point(384, 337), 64, 70);
                }
                else foreach (double x in new[] { 111d, 401d }) drawing.DrawEllipse(Brushes.CadetBlue, outline, new Point(x, 366), 39, 67);
                foreach (double x in side ? new[] { 252d } : new[] { 194d, 322d })
                    if (eye == 0) drawing.DrawEllipse(Brushes.DarkSlateGray, null, new Point(x, side ? 153 : 197), 12, 14);
                    else drawing.DrawLine(new Pen(Brushes.DarkSlateGray, 5), new Point(x - 11, side ? 153 : 197), new Point(x + 11, side ? 153 : 197));
                drawing.DrawEllipse(Brushes.DarkSlateGray, null, new Point(side ? 178 : 256, side ? 203 : 239), 15, 2 + mouth * 3);
                drawing.Pop(); drawing.Pop();
            }
        var bitmap = new RenderTargetBitmap(columns * 128, 256, 96, 96, PixelFormats.Pbgra32); bitmap.Render(sheet);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var file = File.Create(path); encoder.Save(file);
    }
    public static async Task RunAsync(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-companion-qa-" + Guid.NewGuid()), characters = Path.Combine(folder, "Characters"); Directory.CreateDirectory(characters);
        foreach (var item in CharacterCatalog.All) SaveCalibration(Path.Combine(characters, item.FileName), item.SideView);
        var originals = CharacterCatalog.All.ToDictionary(c => c.Id, c => SHA256.HashData(File.ReadAllBytes(Path.Combine(characters, c.FileName))));
        int inputCalls = 0;
        MainViewModel Model(bool fresh = false) => new(true, () => [], _ => { inputCalls++; throw new InvalidOperationException(); }, charactersFolder: characters, dataFolder: folder, initialSettings: fresh ? new(new()) : null);
        var model = Model(true); var window = new MainWindow(true, SystemParameters.WorkArea, model); MainWindow? restarted = null; Action? resume = null;
        try
        {
            window.Show(); await window.Ready; resume = model.PauseVisualForQa();
            window.Width = Math.Min(1100, SystemParameters.WorkArea.Width - 32); window.Height = Math.Min(760, SystemParameters.WorkArea.Height - 32);
            await window.Studio.RefreshAsync();
            check(window.Studio.Cards.Count == 2 && window.Studio.Cards.All(c => c.Legacy != null && !c.Sample), "companion_catalog_has_only_original_two_characters");
            window.ShowStudio(1); await ScreenEvidence.CaptureAsync(window, "companion-library", check);
            foreach (var item in CharacterCatalog.All)
            {
                window.ShowStudio(1); window.StudioSurface.LibraryList.SelectedItem = window.Studio.Cards.Single(c => c.Legacy?.Id == item.Id); window.Studio.SelectCommand.Execute(null);
                await WaitAsync(() => model.SelectedCharacter.Id == item.Id && model.Character.Name.StartsWith(item.Name) && model.CanChooseCharacter);
                var sprite = model.Character.Sprite; var scene = model.Character.CompanionScene;
                check(!model.Packages.IsActive && !model.Runtime.IsActive && model.Character.CompanionCharacterId == item.Id && model.Character.HasCompanionSamples, "companion_direct_catalog_selection_" + item.Id);
                await window.Studio.UseSamplesAsync();
                check(model.SelectedCharacter.Id == item.Id && ReferenceEquals(sprite, model.Character.Sprite) && !model.Packages.IsActive && model.Packages.Entries.Count == 0, "companion_sample_navigation_preserves_character_and_no_library_copy_" + item.Id);
                check(window.Studio.AttachmentCards.Count == 17 && window.Studio.LiveActions.Count == 9 && window.Studio.LiveAttachments.Select(a => a.Category).Distinct().Count() == 14, "companion_all_samples_available_" + item.Id);
                double now = scene.Runtime.Actions.LastTime;
                void Tick(double step = .1, double voice = 0, bool closed = false)
                {
                    now = Math.Max(now, model.Character.CompanionScene.Runtime.Actions.LastTime) + step;
                    model.Hands.Update(now); model.Character.Update(new(voice, 0, 0, 0, closed ? 0 : 1, voice > .6 ? MouthState.Open : MouthState.Closed), now);
                }
                foreach (var card in window.Studio.AttachmentCards.ToArray())
                {
                    card.ToggleCommand.Execute(null); Tick();
                    check(card.IsWorn && card.WearLabel == "해제" && ReferenceEquals(scene, model.Character.RuntimeScene) && scene.Frame.Layers.Any(l => l.Id == "part-head"), "companion_wear_preserves_original_" + item.Id + "_" + card.Id);
                    check(card.Image is BitmapSource { IsFrozen: true, PixelWidth: 160 }, "companion_readable_thumbnail_" + item.Id + "_" + card.Id);
                    await ScreenEvidence.CaptureAsync(window, "companion-" + item.Id + "-costume-" + card.Id.ToLowerInvariant(), check);
                    card.ToggleCommand.Execute(null); check(!card.IsWorn, "companion_remove_" + item.Id + "_" + card.Id);
                }
                var top = window.Studio.AttachmentCards.Single(c => c.Id == "top"); top.ToggleCommand.Execute(null);
                window.Studio.Page = 3;
                var actionPoses = new HashSet<(double Left, double Right)>();
                foreach (var action in window.Studio.LiveActions)
                {
                    Tick(1.4); string rest = UnifiedStudioQa.Render(model.Character);
                    Find<Button>(window.StudioSurface).Single(b => b.Tag is string tag && tag == action.Id).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Tick(0); Tick(.2);
                    check(scene.Runtime.State.ActionId == action.Id && UnifiedStudioQa.Render(model.Character) != rest && scene.Frame.Layers.Any(l => l.Id == "part-arm-right" && l.Matrix != VoxPet.Core.Character.AffineTransform2D.Identity), "companion_action_moves_original_" + item.Id + "_" + action.Id);
                    check(scene.Frame.Layers.Single(l => l.Id == "part-arm-right").ZIndex > scene.Frame.Layers.Single(l => l.Id == "part-body").ZIndex, "companion_front_wing_layer_" + item.Id + "_" + action.Id);
                    actionPoses.Add((model.Character.Motion.LeftAngle, model.Character.Motion.RightAngle));
                    await ScreenEvidence.CaptureAsync(window, "companion-" + item.Id + "-action-" + action.Id, check);
                    Tick(1.3); check(scene.Runtime.State.ActionId == null && top.IsWorn, "companion_action_idle_keeps_clothes_" + item.Id + "_" + action.Id);
                }
                check(actionPoses.Count == 9, "companion_nine_distinct_action_poses_" + item.Id);
                window.Studio.RemoveAttachmentsCommand.Execute(null); window.Studio.Page = 2;
                foreach (string id in new[] { "hat", "cape" }) window.Studio.AttachmentCards.Single(c => c.Id == id).ToggleCommand.Execute(null);
                foreach (string hair in new[] { "hair-short", "hair-long", "hair-afro" })
                {
                    window.Studio.AttachmentCards.Single(c => c.Id == hair).ToggleCommand.Execute(null); Tick();
                    check(scene.Runtime.Attachments.EnabledIds.Count == 3 && scene.Runtime.Attachments.EnabledIds.Contains(hair), "companion_hair_hat_wings_" + item.Id + "_" + hair);
                    check(ClearFace(scene.Images["samples/" + hair + "-fringe.png"], item.SideView), "companion_hair_keeps_eyes_and_mouth_clear_" + item.Id + "_" + hair);
                    await ScreenEvidence.CaptureAsync(window, "companion-" + item.Id + "-" + hair, check);
                }
                var animated = new HashSet<string>();
                for (int frame = 0; frame < 4; frame++)
                {
                    Tick(.16); animated.Add(UnifiedStudioQa.Render(model.Character));
                    check(new[] { "hat", "cape" }.All(id => scene.Runtime.Definition.Attachments.Single(a => a.Id == id).Layers.All(l => scene.Frame.Layers.Any(r => r.Asset == l.Animation!.FrameAt(now)))), "companion_animated_frame_" + item.Id + "_" + frame);
                    await ScreenEvidence.CaptureAsync(window, "companion-" + item.Id + "-animation-" + frame, check);
                }
                check(animated.Count == 4, "companion_four_distinct_frames_" + item.Id);
                var broadcast = window.ShowCharacter();
                check(ReferenceEquals(broadcast.DataContext, model) && ReferenceEquals(model.Character.RuntimeScene, scene), "companion_broadcast_shares_original_scene_" + item.Id);
                await ScreenEvidence.CaptureAsync(broadcast, "companion-" + item.Id + "-broadcast", check); broadcast.Close();
                Tick(.1, 1, true);
                check(model.Character.MouthFrameCount == (item.SideView ? 3 : 8) && model.Character.IsBlinking && scene.Frame.Layers.Any(l => l.Asset == VoxPet.Core.Legacy.CompanionParts.Head(item.SideView ? 2 : 7, 0)), "companion_original_mouth_and_blink_" + item.Id);
                var selected = scene.Runtime.Attachments.EnabledIds.ToArray();
                model.Character.RestoreDefault(); Tick(); scene = model.Character.CompanionScene;
                check(selected.SequenceEqual(scene.Runtime.Attachments.EnabledIds) && window.Studio.AttachmentCards.Count == 17 && ReferenceEquals(scene, window.Studio.DisplayScene), "companion_expression_rebuild_keeps_clothes_and_buttons_" + item.Id);
                window.Studio.EditCompanionCommand.Execute(null);
                await WaitAsync(() => window.Studio.Page == 5 && window.Studio.Editor.Document?.Package.Manifest.Id == "companion." + item.Id && !window.Studio.Editor.IsBusy);
                window.Studio.Editor.PausePreview(); window.Studio.Editor.Page = 6;
                check(window.Studio.Editor.Document?.Package.Manifest.Id == "companion." + item.Id && window.Studio.Editor.Document.Package.Character.SpriteSheet != null, "companion_editor_uses_original_sheet_" + item.Id);
                window.Studio.Editor.Selected = window.Studio.Editor.Rows.Single(r => r.Id == "hat");
                window.Studio.Editor.DetailFields.Single(f => f.Key == "animationMs").Value = "120"; await window.Studio.Editor.ApplyDetailAsync();
                string exported = Path.Combine(folder, item.Id + "-edited.voxpet"); check(await window.Studio.Editor.ExportAsync(exported), "companion_editor_export_" + item.Id);
                var reloaded = new VoxPetPackageLoader().Load(exported);
                check(reloaded.Character.Attachments.Single(a => a.Id == "hat").Layers[0].Animation!.FrameDurationMs == 120 && reloaded.Character.Actions.Count == 9 && reloaded.AssetPaths.Any(p => p.StartsWith("legacy/sprite-", StringComparison.Ordinal)), "companion_editor_reload_preserves_original_and_animation_" + item.Id);
                await ScreenEvidence.CaptureAsync(window, "companion-" + item.Id + "-editor", check);
                check(SHA256.HashData(File.ReadAllBytes(Path.Combine(characters, item.FileName))).SequenceEqual(originals[item.Id]), "companion_original_file_preserved_" + item.Id);
                window.ShowStudio(2); window.Studio.PlayAction("heart"); Tick(0); Tick(.2); window.Studio.StopActionsCommand.Execute(null); Tick();
                check(scene.Runtime.State.ActionId == null, "companion_stop_action_" + item.Id);
            }
            check(await model.SelectCharacterAsync("neulbo", true) && model.Character.CompanionScene.Runtime.Attachments.EnabledIds.Count == 3, "companion_switch_restores_character_own_clothes");
            await model.SelectCharacterAsync("pelly", true);
            check(await model.Packages.ImportAsync(StudioShellViewModel.SamplePath), "companion_old_automatic_sample_fixture");
            string oldId = model.Packages.Loaded!.LibraryId, oldPath = Path.Combine(folder, "CharactersV2", oldId + ".voxpet");
            byte[] oldBytes = File.ReadAllBytes(oldPath);
            await window.Studio.UseSamplesAsync();
            check(!model.Packages.IsActive && model.SelectedCharacter.Id == "pelly" && model.Character.Name.StartsWith("펠리"), "companion_samples_exit_old_standin_to_selected_character");
            await model.Packages.SelectAsync(oldId); await window.ShutdownAsync(); window.Close();
            var restored = Model(); restarted = new MainWindow(true, SystemParameters.WorkArea, restored); restarted.Show(); await restarted.Ready; restarted.ShowStudio(2);
            check(restored.SelectedCharacter.Id == "pelly" && restored.Character.Name.StartsWith("펠리") && restored.Character.HasCompanionSamples && restarted.Studio.AttachmentCards.Count == 17 && !restored.Packages.IsActive, "companion_restart_keeps_character_and_builtin_samples");
            check(oldBytes.SequenceEqual(File.ReadAllBytes(oldPath)) && restored.Packages.Entries.Any(e => e.LibraryId == oldId) && restarted.Studio.Cards.All(c => c.Id != StudioShellViewModel.SampleId), "companion_upgrade_hides_standin_preserves_managed_file");
            await ScreenEvidence.CaptureAsync(restarted, "companion-restarted", check);
            check(inputCalls == 0, "companion_never_starts_microphone");
        }
        finally
        {
            if (restarted != null) { await restarted.ShutdownAsync(); restarted.Close(); }
            resume?.Invoke(); await window.ShutdownAsync(); window.Close(); Directory.Delete(folder, true);
        }
    }
}
