using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using VoxPet.Core.Rendering;

namespace VoxPet.App.Views;

/// <summary>실제 UI의 파츠/헤어/무기 선택과 재시작을 공개 교정 도형으로 시험한다.</summary>
internal static class PartWardrobeQa
{
    private static IEnumerable<T> Find<T>(DependencyObject node) where T : DependencyObject
    { if (node is T item) yield return item; for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Find<T>(VisualTreeHelper.GetChild(node, i))) yield return child; }
    private static byte[] Pixels(Action<DrawingContext> draw)
    {
        var visual = new DrawingVisual(); using (var drawing = visual.RenderOpen()) draw(drawing);
        var image = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32); image.Render(visual);
        var pixels = new byte[512 * 512 * 4]; image.CopyPixels(pixels, 512 * 4, 0); return pixels;
    }
    public static async Task RunAsync(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-parts-qa-" + Guid.NewGuid()), characters = Path.Combine(folder, "Characters"); Directory.CreateDirectory(characters);
        foreach (var entry in CharacterCatalog.All) CompanionSamplesQa.SaveCalibration(Path.Combine(characters, entry.FileName), entry.SideView);
        int inputCalls = 0;
        MainViewModel Model() => new(true, () => [], _ => { inputCalls++; throw new InvalidOperationException(); }, charactersFolder: characters, dataFolder: folder);
        var model = Model(); var window = new MainWindow(true, SystemParameters.WorkArea, model); MainWindow? restart = null; Action? resume = null;
        try
        {
            window.Show(); await window.Ready; resume = model.PauseVisualForQa();
            window.Width = Math.Min(1100, SystemParameters.WorkArea.Width - 32); window.Height = Math.Min(760, SystemParameters.WorkArea.Height - 32);
            foreach (var entry in CharacterCatalog.All)
            {
                check(await model.SelectCharacterAsync(entry.Id, true), "parts_select_" + entry.Id); window.ShowStudio(2);
                var scene = model.Character.CompanionScene; double now = scene.Runtime.Actions.LastTime;
                void Tick(double step = .1)
                { now = Math.Max(now, model.Character.CompanionScene.Runtime.Actions.LastTime) + step; model.Hands.Update(now); model.Character.Update(new(0, 0, 0, 0, 1, MouthState.Closed), now); }
                Tick(.4);
                check(scene.Runtime.Definition.Parts.Count == 6 && scene.Frame.Layers.Count(l => l.Id.StartsWith("part-", StringComparison.Ordinal)) == 6 && scene.Frame.Layers.All(l => l.Id != "legacy-sprite"), "parts_six_independent_layers_" + entry.Id);
                var solid = BitmapSource.Create(512, 512, 96, 96, PixelFormats.Pbgra32, null, Enumerable.Repeat((byte)255, 512 * 512 * 4).ToArray(), 512 * 4); solid.Freeze();
                var torso = CompanionPartImages.Cut(solid, "body", entry.SideView);
                byte[] body = await Task.Run(() => CompanionPartImages.Encode(torso));
                using (var stream = new MemoryStream(body, false))
                {
                    var bitmap = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0]; var pixel = new byte[4];
                    bitmap.CopyPixels(new Int32Rect(256, 470, 1, 1), pixel, 4, 0);
                    check(pixel[3] == 255, "parts_body_keeps_pixels_between_feet_" + entry.Id);
                }
                foreach (var part in CompanionParts.Ids)
                {
                    string asset = part == "head" ? CompanionParts.Head(0, 1) : CompanionParts.Asset(part);
                    var image = scene.Images[asset]; byte[] bytes = await Task.Run(() => CompanionPartImages.Encode(image));
                    check(image is BitmapSource { IsFrozen: true, PixelWidth: 512, PixelHeight: 512 } && bytes.Length > 60 && bytes[0] == 137, "parts_worker_png_export_" + entry.Id + "_" + part);
                    if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" && Environment.GetEnvironmentVariable("RUNNER_TEMP") is { } temp)
                        File.WriteAllBytes(Path.Combine(temp, "voxpet-part-" + entry.Id + "-" + part + ".png"), bytes);
                }
                var original = Pixels(d => d.DrawImage(scene.Images[LegacySpriteSheetAdapter.Asset(0, 1)], new Rect(0, 0, 512, 512)));
                var rebuilt = Pixels(d => RenderFrameDrawing.Draw(d, new RenderFrame(scene.Frame.Layers.Where(l => l.Id.StartsWith("part-", StringComparison.Ordinal))), scene.Images));
                int changed = Enumerable.Range(0, 512 * 512).Count(i => Enumerable.Range(0, 4).Any(c => Math.Abs(original[i * 4 + c] - rebuilt[i * 4 + c]) > 24));
                check(changed < 512 * 512 / 100, "parts_rest_pixels_preserve_original_" + entry.Id);
                if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" && Environment.GetEnvironmentVariable("RUNNER_TEMP") is { } pixelFolder)
                    File.WriteAllText(Path.Combine(pixelFolder, "voxpet-runtime-part-pixels-" + entry.Id + ".json"), System.Text.Json.JsonSerializer.Serialize(new { changed, total = 512 * 512, tolerance = 24, maxChangedFraction = .01, layers = scene.Frame.Layers.Select(l => new { l.Id, l.Opacity, l.Matrix, mesh = l.Mesh != null }) }));
                foreach (string costume in new[] { "top", "bottom", "shoes" }) window.Studio.AttachmentCards.Single(c => c.Id == costume).ToggleCommand.Execute(null);
                Tick();
                await ScreenEvidence.CaptureAsync(window, "parts-" + entry.Id + "-separated", check);
                check(scene.Frame.Layers.Single(l => l.Asset == "samples/top-right.png").Matrix == scene.Frame.Layers.Single(l => l.Id == "part-arm-right").Matrix, "parts_sleeve_and_arm_same_transform_" + entry.Id);
                check(window.Studio.HairCards.Count() == 3 && window.Studio.ClothingCards.Count() == 14, "parts_hair_separate_from_clothing_" + entry.Id);
                foreach (string hair in new[] { "hair-short", "hair-long", "hair-afro" })
                {
                    var card = window.Studio.HairCards.Single(c => c.Id == hair);
                    var button = Find<Button>(window.StudioSurface).Single(b => ReferenceEquals(b.DataContext, card));
                    var invoke = (System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(button).GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke);
                    invoke.Invoke(); await Task.Delay(20);
                    Tick(); check(card.IsWorn && button.IsVisible && scene.Runtime.Attachments.EnabledIds.Contains(hair), "parts_hair_visible_and_worn_" + entry.Id + "_" + hair);
                    await ScreenEvidence.CaptureAsync(window, "parts-" + entry.Id + "-" + hair, check);
                }
                window.ShowStudio(3);
                var selector = Find<ComboBox>(window.StudioSurface).Single(c => System.Windows.Automation.AutomationProperties.GetName(c) == "장착 무기 선택");
                var toggle = Find<CheckBox>(window.StudioSurface).Single(c => System.Windows.Automation.AutomationProperties.GetName(c) == "무기 항상 들고 있기");
                for (int index = 0; index < CompanionWeapon.All.Count; index++)
                {
                    var weapon = CompanionWeapon.All[index]; selector.SelectedIndex = index + 1; toggle.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, true); Tick();
                    check(model.Equipment.WeaponIndex == index + 1 && scene.Frame.Layers.Single(l => l.Id == "companion-equipped-weapon").Asset == weapon.Asset, "parts_weapon_ui_equipped_idle_" + entry.Id + "_" + weapon.Id);
                    await ScreenEvidence.CaptureAsync(window, "parts-" + entry.Id + "-weapon-" + weapon.Id + "-idle", check);
                    toggle.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, false); Tick();
                    check(!model.Equipment.HoldWeapon && scene.Frame.Layers.All(l => l.Id != "companion-equipped-weapon"), "parts_weapon_ui_hold_off_" + entry.Id + "_" + weapon.Id);
                    window.Studio.PlayAction(weapon.Id); Tick(0); Tick(.2);
                    check(scene.Runtime.State.ActionId == weapon.Id && scene.Frame.Layers.Count(l => l.Asset == weapon.Asset) == 1, "parts_weapon_once_during_action_" + entry.Id + "_" + weapon.Id);
                    await ScreenEvidence.CaptureAsync(window, "parts-" + entry.Id + "-weapon-" + weapon.Id + "-action", check);
                    Tick(1.4); check(scene.Frame.Layers.All(l => l.Id != "companion-equipped-weapon"), "parts_weapon_off_after_idle_" + entry.Id + "_" + weapon.Id);
                }
                await ScreenEvidence.CaptureAsync(window, "parts-" + entry.Id + "-weapon-off", check);
                toggle.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, true); Tick();
                window.Studio.PlayAction("claw"); Tick(0); Tick(.3);
                check(window.Studio.LiveActions.Single(a => a.Id == "claw").Name == "법규" && scene.Frame.Layers.Any(l => l.Asset == "samples/claw.png") && scene.Frame.Layers.All(l => l.Id != "companion-equipped-weapon"), "parts_law_actual_hand_replaces_claw_effect_" + entry.Id);
                await ScreenEvidence.CaptureAsync(window, "parts-" + entry.Id + "-law", check);
                Tick(1.4); check(scene.Frame.Layers.Any(l => l.Id == "companion-equipped-weapon"), "parts_weapon_returns_after_law_" + entry.Id);
                model.Equipment.WeaponIndex = entry.Id == "neulbo" ? 1 : 5; model.Equipment.HoldWeapon = entry.Id != "neulbo"; Tick();
                var package = await model.Character.EditableCompanionAsync();
                check(package?.Character.Parts.Count == 6 && package.Character.Parts.All(p => p.Asset.StartsWith("parts/", StringComparison.Ordinal)), "parts_editor_exports_actual_separated_png_" + entry.Id);
                var editor = window.Studio.Editor; check(await editor.OpenPackageAsync(package!), "parts_editor_opens_separated_" + entry.Id); editor.PausePreview(); editor.Page = 1;
                editor.Selected = editor.Rows.Single(r => r.Id == "arm-right"); await editor.ApplyAsync();
                check(editor.Document!.Package.Character.Parts.Single(p => p.Id == "arm-right").Skinning == package!.Character.Parts.Single(p => p.Id == "arm-right").Skinning, "parts_editor_part_edit_keeps_joint_" + entry.Id);
                editor.Page = 6; editor.Selected = editor.Rows.Single(r => r.Id == "top");
                editor.SelectedDetail = editor.Details.Single(r => r.Id == "right"); await editor.ApplyDetailAsync();
                string exported = Path.Combine(folder, entry.Id + "-parts.voxpet"); check(await editor.ExportAsync(exported), "parts_editor_joint_export_" + entry.Id);
                var reloaded = new VoxPetPackageLoader().Load(exported); var playback = reloaded.CreateRuntime(); playback.Attachments.SetEnabled("top", true); playback.RequestAction("toy-gun"); playback.Update(new(0, 0, 0));
                var frame = playback.Update(new(.2, .2, 0));
                check(frame.Layers.Single(l => l.Id == "part-arm-right").Mesh != null && ReferenceEquals(frame.Layers.Single(l => l.Id == "part-arm-right").Mesh, frame.Layers.Single(l => l.Asset == "samples/top-right.png").Mesh), "parts_editor_reload_shares_actual_joint_" + entry.Id);
            }
            await window.ShutdownAsync(); window.Close();
            var saved = new VoxPet.Core.Services.CompanionEquipmentStore(Path.Combine(folder, "companion-equipment.json")).Load();
            check(saved.GetValueOrDefault("neulbo") == new CompanionEquipment("toy-gun", false) && saved.GetValueOrDefault("pelly") == new CompanionEquipment("bow", true), "parts_equipment_both_characters_saved");
            var restored = Model(); restart = new MainWindow(true, SystemParameters.WorkArea, restored); restart.Show(); await restart.Ready; restart.ShowStudio(3);
            check(restored.SelectedCharacter.Id == "pelly" && restored.Equipment.WeaponIndex == 5 && restored.Equipment.HoldWeapon && restored.Character.CompanionScene.Frame.Layers.Any(l => l.Id == "companion-equipped-weapon"), "parts_equipment_restart_restored");
            await ScreenEvidence.CaptureAsync(restart, "parts-equipment-restart", check);
            check(inputCalls == 0, "parts_equipment_no_microphone");
        }
        finally
        {
            if (restart != null) { await restart.ShutdownAsync(); restart.Close(); }
            resume?.Invoke(); await window.ShutdownAsync(); window.Close(); Directory.Delete(folder, true);
        }
    }
}
