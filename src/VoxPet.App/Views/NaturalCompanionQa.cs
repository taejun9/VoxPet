using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using VoxPet.Core.Rendering;

namespace VoxPet.App.Views;

internal static class NaturalCompanionQa
{
    private static IEnumerable<T> Find<T>(DependencyObject node) where T : DependencyObject
    { if (node is T item) yield return item; for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Find<T>(VisualTreeHelper.GetChild(node, i))) yield return child; }
    private static void Invoke(Button button) => ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(button).GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)).Invoke();
    private static void Seams(string id, bool side, Action<bool, string> check)
    {
        var solid = BitmapSource.Create(512, 512, 96, 96, PixelFormats.Pbgra32, null, Enumerable.Repeat((byte)255, 512 * 512 * 4).ToArray(), 2048); solid.Freeze();
        var sheet = new ImageSource[3, 2]; for (int m = 0; m < 3; m++) for (int e = 0; e < 2; e++) sheet[m, e] = solid;
        var images = CompanionPartImages.Create(sheet, side);
        var p = new VoxPetPackageLoader().Load(Path.Combine(AppContext.BaseDirectory, "Assets", "Samples", "companion-" + id + ".voxpet"));
        var runtime = new CharacterRuntime(LegacySpriteSheetAdapter.Convert(3, samples: p.Character));
        runtime.RequestAction("firearm"); runtime.Update(new(0, 0, 0));
        foreach (double t in new[] { 0d, .26, .38, .405, .52 })
        {
            var frame = runtime.Update(new(t, .01, 0));
            var root = runtime.CurrentRig.Animated["body"].Compose(runtime.CurrentRig.Static["body"].Inverse());
            foreach (int size in new[] { 128, 256, 512 })
            {
                var visual = new DrawingVisual(); RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased); RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
                using (var drawing = visual.RenderOpen()) RenderFrameDrawing.Draw(drawing, new RenderFrame(frame.Layers.Where(l => l.Id.StartsWith("part-", StringComparison.Ordinal))), images, width: size, height: size);
                var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); var pixels = new byte[size * size * 4]; bitmap.CopyPixels(pixels, size * 4, 0);
                bool opaque = true;
                foreach (var point in side ? new[] { new RigPoint(312, 302), new RigPoint(271, 299) } : new[] { new RigPoint(145, 315), new RigPoint(367, 315), new RigPoint(256, 285) })
                {
                    var mapped = root.Map(point.X, point.Y); int cx = (int)(mapped.X * size / 512), cy = (int)(mapped.Y * size / 512);
                    for (int y = cy - 1; y <= cy + 1; y++) for (int x = cx - 1; x <= cx + 1; x++) if (pixels[(y * size + x) * 4 + 3] < 248) opaque = false;
                }
                check(opaque, $"natural_joint_opaque_{id}_{size}_{(int)(t * 1000)}");
            }
        }
    }
    public static async Task RunAsync(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-natural-qa-" + Guid.NewGuid()), characters = Path.Combine(folder, "Characters"); Directory.CreateDirectory(characters);
        foreach (var item in CharacterCatalog.All) CompanionSamplesQa.SaveCalibration(Path.Combine(characters, item.FileName), item.SideView);
        int inputCalls = 0; var model = new MainViewModel(false, () => [], _ => { inputCalls++; throw new InvalidOperationException(); }, charactersFolder: characters, dataFolder: folder);
        var window = new MainWindow(true, SystemParameters.WorkArea, model); Action? resume = null;
        try
        {
            window.Show(); await window.Ready; resume = model.PauseVisualForQa(); window.Width = Math.Min(1100, SystemParameters.WorkArea.Width - 32); window.Height = Math.Min(760, SystemParameters.WorkArea.Height - 32);
            foreach (var item in CharacterCatalog.All)
            {
                check(await model.SelectCharacterAsync(item.Id, true), "natural_select_" + item.Id); Seams(item.Id, item.SideView, check);
                double now = model.Character.CompanionScene.Runtime.Actions.LastTime;
                void Tick(double dt = .1) { now = Math.Max(now, model.Character.CompanionScene.Runtime.Actions.LastTime) + dt; model.Hands.Update(now); model.Character.Update(new(0, 0, 0, 0, 1, MouthState.Closed), now); }
                window.ShowStudio(2); window.UpdateLayout();
                var selector = Find<ComboBox>(window.StudioSurface).Single(c => System.Windows.Automation.AutomationProperties.GetName(c) == "의상 분류");
                foreach (var category in window.Studio.ClothingCategories)
                {
                    selector.SelectedValue = category.Key; await Task.Delay(10); window.UpdateLayout();
                    check(window.Studio.ClothingCategory == category.Key && window.Studio.FilteredClothingCards.Count() == (category.Key == "all" ? 14 : 1) && window.Studio.FilteredClothingCards.All(c => category.Key == "all" || c.Category == category.Key), "natural_category_" + item.Id + "_" + category.Key);
                }
                selector.SelectedValue = "top"; await ScreenEvidence.CaptureAsync(window, "natural-" + item.Id + "-categories", check); selector.SelectedValue = "all";
                foreach (string garment in new[] { "top", "bottom", "shoes" }) window.Studio.AttachmentCards.Single(c => c.Id == garment).ToggleCommand.Execute(null);
                model.Equipment.WeaponIndex = 1; model.Equipment.HoldWeapon = true; Tick(.4);
                window.Studio.NavigationIndex = 2; window.UpdateLayout();
                check(window.Studio.Page == 6 && window.Studio.IsExpressions && Find<ItemsControl>(window.StudioSurface).Any(c => c.IsVisible && System.Windows.Automation.AutomationProperties.GetName(c) == "표정 슬롯 선택"), "natural_expression_sidebar_visible_" + item.Id);
                foreach (var slot in model.Expressions.Slots)
                {
                    var button = Find<Button>(window.StudioSurface).Single(b => ReferenceEquals(b.DataContext, slot) && b.IsVisible); Invoke(button); await Task.Delay(20); Tick(.4);
                    check(model.Expressions.Selected == slot && model.Character.ExpressionKind == slot.Saved.Kind && model.Character.CompanionScene.Runtime.Attachments.EnabledIds.Count == 3 && model.Character.CompanionScene.Frame.Layers.Any(l => l.Id == "companion-equipped-weapon"), "natural_expression_slot_keeps_outfit_" + item.Id + "_" + slot.Position);
                }
                await ScreenEvidence.CaptureAsync(window, "natural-" + item.Id + "-expressions", check);
                foreach (var size in new[] { new Size(1280, 820), new Size(960, 540), new Size(640, 480), new Size(480, 320) })
                {
                    window.Width = Math.Min(size.Width, SystemParameters.WorkArea.Width - 32); window.Height = Math.Min(size.Height, SystemParameters.WorkArea.Height - 32); window.UpdateLayout();
                    check(window.Studio.NavigationIndex == 2 && window.Studio.Navigation.Length == 7 && window.Studio.IsExpressions, $"natural_expression_navigation_{item.Id}_{(int)size.Width}");
                    await ScreenEvidence.CaptureAsync(window, $"natural-{item.Id}-expression-{(int)size.Width}", check);
                }
                window.Width = Math.Min(1100, SystemParameters.WorkArea.Width - 32); window.Height = Math.Min(760, SystemParameters.WorkArea.Height - 32); window.UpdateLayout();
                var edit = Find<Button>(window.StudioSurface).Single(b => b.IsVisible && b.Content is string text && text == "슬롯 편집 / PNG 등록"); Invoke(edit); await Task.Delay(20);
                check(window.Studio.Page == 4 && model.SettingsTab == 3 && window.SettingsTabs.IsVisible && !window.LayoutRoot.IsVisible, "natural_expression_editor_same_shell_" + item.Id);
                window.ShowStudio(3); model.Equipment.HoldWeapon = false;
                check(!window.Studio.HasLegacyQuickActions && Find<Button>(window.StudioSurface).Where(b => b.Tag is string tag && int.TryParse(tag, out int n) && n is >= 0 and <= 4).All(b => !b.IsVisible), "natural_no_duplicate_legacy_gestures_" + item.Id);
                foreach (var weapon in CompanionWeapon.All)
                {
                    window.Studio.PlayAction(weapon.Id); Tick(0); double previous = 0;
                    foreach (double t in new[] { .26, .38, .405, .59, .76 })
                    {
                        Tick(t - previous); previous = t;
                        var scene = model.Character.CompanionScene; var matrix = scene.Frame.Layers.Single(l => l.Id == "companion-equipped-weapon").Matrix;
                        var rest = scene.Runtime.CurrentRig.Static[weapon.Anchor].Map(0, 0); var actual = matrix.Map(rest.X, rest.Y); var hand = scene.Runtime.CurrentRig.Animated[weapon.Anchor].Map(0, 0);
                        check(Math.Abs(actual.X - hand.X) < .01 && Math.Abs(actual.Y - hand.Y) < .01 && scene.Frame.Layers.Any(l => l.Asset == "samples/grip-" + (weapon.Anchor == "leftHand" ? "left" : "right") + ".png"), $"natural_grip_contact_{item.Id}_{weapon.Id}_{(int)(t * 1000)}");
                        await ScreenEvidence.CaptureAsync(window, $"natural-{item.Id}-{weapon.Id}-{(int)(t * 1000)}", check);
                    }
                    Tick(1.4); check(model.Character.CompanionScene.Runtime.State.ActionId == null, "natural_recover_" + item.Id + "_" + weapon.Id);
                }
                window.Studio.PlayAction("firearm"); Tick(0); Tick(.38);
                check(model.Character.CompanionScene.Frame.Layers.Any(l => l.Asset == "samples/muzzle-flash.png"), "natural_shoot_before_unequip_" + item.Id);
                model.Equipment.WeaponIndex = 0; Tick(0);
                check(model.Character.CompanionScene.Runtime.State.ActionId == null && model.Character.CompanionScene.Frame.Layers.All(l => l.Asset != "samples/muzzle-flash.png" && !l.Id.StartsWith("companion-equipped-", StringComparison.Ordinal)), "natural_unequip_stops_old_action_" + item.Id);
                window.Studio.RemoveAttachmentsCommand.Execute(null);
            }
            check(inputCalls == 0, "natural_no_microphone");
        }
        finally { resume?.Invoke(); await window.ShutdownAsync(); window.Close(); Directory.Delete(folder, true); }
    }
}
