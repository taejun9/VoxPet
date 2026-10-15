using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.ViewModels;

namespace VoxPet.App.Views;

internal static class UnifiedStudioQa
{
    private static IEnumerable<T> Find<T>(DependencyObject node) where T : DependencyObject
    { if (node is T item) yield return item; for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Find<T>(VisualTreeHelper.GetChild(node, i))) yield return child; }
    private static string Render(CharacterViewModel character)
    {
        var surface = new MotionSurface { DataContext = character }; surface.Measure(new Size(512, 512)); surface.Arrange(new Rect(0, 0, 512, 512)); surface.UpdateLayout();
        var image = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32); image.Render(surface);
        var pixels = new byte[512 * 512 * 4]; image.CopyPixels(pixels, 2048, 0); return Convert.ToHexString(SHA256.HashData(pixels));
    }
    public static async Task RunAsync(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-studio-qa-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        int inputCalls = 0;
        var model = new MainViewModel(true, () => [], _ => { inputCalls++; throw new InvalidOperationException(); }, dataFolder: folder, charactersFolder: Path.Combine(folder, "missing"), initialSettings: new(new()));
        var window = new MainWindow(true, SystemParameters.WorkArea, model); Action? resume = null;
        try
        {
            window.Show(); await window.Ready; resume = model.PauseVisualForQa(); window.ShowStudio();
            check(window.StudioSurface.IsVisible && !window.LayoutRoot.IsVisible && ReferenceEquals(window.SettingsTabs.Parent, window.StudioSurface.SettingsHost), "studio_single_shell_mounts_settings");
            await window.Studio.RefreshAsync(); check(window.Studio.Cards.Count(c => c.Sample) == 1, "studio_sample_card_bundled");
            await window.Studio.UseSamplesAsync();
            check(model.Packages.IsActive && window.Studio.AttachmentCards.Count == 14 && window.Studio.LiveActions.Count == 9, "studio_sample_ready_without_user_files");
            var scene = model.Packages.Scene!; double now = scene.Runtime.Actions.LastTime;
            await window.Studio.UseSamplesAsync(); check(model.Packages.Entries.Count(e => e.CharacterId == StudioShellViewModel.SampleId) == 1, "studio_sample_reselect_reuses_managed_copy");
            scene = model.Packages.Scene!; now = scene.Runtime.Actions.LastTime;
            window.Width = Math.Min(1280, SystemParameters.WorkArea.Width - 40); window.Height = Math.Min(820, SystemParameters.WorkArea.Height - 40); window.Left = 20; window.Top = 20;
            foreach (var card in window.Studio.AttachmentCards)
            {
                card.ToggleCommand.Execute(null); model.Packages.Update(now += .1, 0);
                check(card.IsWorn && card.WearLabel == "해제" && scene.Frame.Layers.Any(l => l.Asset == "samples/" + card.Id + ".png"), "studio_wear_" + card.Id);
                Render(model.Character);
                await ScreenEvidence.CaptureAsync(window, "studio-costume-" + card.Id.ToLowerInvariant(), check);
                card.ToggleCommand.Execute(null); check(!card.IsWorn, "studio_remove_" + card.Id);
            }
            var top = window.Studio.AttachmentCards.Single(c => c.Id == "top"); top.ToggleCommand.Execute(null);
            var suit = window.Studio.AttachmentCards.Single(c => c.Id == "onePiece"); suit.ToggleCommand.Execute(null);
            check(suit.IsWorn && !top.IsWorn && top.WearLabel == "착용", "studio_costume_conflicts_refresh_all_cards");
            window.Studio.RemoveAttachmentsCommand.Execute(null); top.ToggleCommand.Execute(null);
            window.Studio.Page = 3; window.UpdateLayout();
            var sampleNames = window.Studio.LiveAttachments.Select(a => a.Name).Concat(window.Studio.LiveActions.Select(a => a.Name)).ToHashSet();
            check(Find<TextBlock>(window.StudioSurface).Where(t => sampleNames.Contains(t.Text)).All(t => t.Foreground is SolidColorBrush b && b.Color.R < 128 && b.Color.G < 128 && b.Color.B < 128), "studio_sample_card_text_contrast");
            foreach (var action in window.Studio.LiveActions)
            {
                model.Packages.Update(now += 2, 0); string rest = Render(model.Character);
                var button = Find<Button>(window.StudioSurface).Single(b => b.Tag is string id && id == action.Id); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                model.Packages.Update(now, 0); model.Packages.Update(now += .2, 0);
                check(scene.Runtime.State.ActionId == action.Id && Render(model.Character) != rest && scene.Frame.Layers.Any(l => l.Mesh != null), "studio_action_with_costume_" + action.Id);
                await ScreenEvidence.CaptureAsync(window, "studio-action-" + action.Id, check);
                model.Packages.Update(now += 1.2, 0); check(scene.Runtime.State.ActionId == null, "studio_action_idle_" + action.Id);
            }
            window.Studio.Page = 3; await window.Studio.UseSamplesAsync(); check(window.Studio.Page == 3, "studio_action_sample_keeps_page");
            window.Studio.AttachmentCards.Single(c => c.Id == "top").ToggleCommand.Execute(null);
            var broadcastScene = model.Packages.Scene!; double broadcastTime = broadcastScene.Runtime.Actions.LastTime;
            model.Packages.Play("heart"); model.Packages.Update(broadcastTime, 0); model.Packages.Update(broadcastTime + .2, 0);
            var broadcast = window.ShowCharacter(); check(ReferenceEquals(model.Character.RuntimeScene, model.Packages.Scene) && ReferenceEquals(broadcast.DataContext, model), "studio_preview_broadcast_shared"); await ScreenEvidence.CaptureAsync(broadcast, "studio-sample-broadcast", check); broadcast.Close();
            foreach (var size in new[] { new Size(1280, 820), new Size(960, 540), new Size(640, 480), new Size(480, 320) })
            {
                window.Width = Math.Min(size.Width, SystemParameters.WorkArea.Width - 32); window.Height = Math.Min(size.Height, SystemParameters.WorkArea.Height - 32);
                for (int page = 0; page < 6; page++)
                {
                    window.Studio.Page = page; window.UpdateLayout(); await Task.Delay(15);
                    check(window.StudioSurface.IsVisible && !window.LayoutRoot.IsVisible, $"studio_single_navigation_{size.Width}_{page}");
                    check(Find<ListBox>(window.StudioSurface).Any(l => ReferenceEquals(l.ItemsSource, window.Studio.Navigation) && l.IsVisible), $"studio_sidebar_retained_{size.Width}_{page}");
                    if (page == 4)
                    {
                        check(window.SettingsTabs.IsVisible && window.StudioSurface.SettingsSection.IsVisible && window.SettingsTabs.DataContext == model, $"studio_settings_embedded_{size.Width}");
                        check(Find<TextBlock>(window.SettingsTabs).Where(t => t.IsVisible && t.Text == "마이크").All(t => t.Foreground is SolidColorBrush b && b.Color.R < 128), $"studio_settings_light_text_{size.Width}");
                    }
                    await ScreenEvidence.CaptureAsync(window, $"studio-page-{(int)size.Width}-{page}", check);
                }
            }
            window.ShowStudioSettings(1); model.Sensitivity = 1.35; window.ShowStudio(0); window.ShowStudioSettings(1);
            check(model.Sensitivity == 1.35 && window.Studio.Page == 4 && !window.LayoutRoot.IsVisible, "studio_settings_retained_across_navigation");
            await model.Packages.DeactivateAsync(); window.ShowStudioSettings(5);
            check(window.Studio.SettingsSections.Length == 7 && model.SettingsTab == 5 && !window.LayoutRoot.IsVisible, "studio_png_settings_same_shell");
            window.Width = Math.Min(1000, SystemParameters.WorkArea.Width - 32); window.Height = Math.Min(730, SystemParameters.WorkArea.Height - 32);
            for (int tab = 0; tab < 7; tab++)
            {
                window.ShowStudioSettings(tab); window.UpdateLayout();
                check(!window.LayoutRoot.IsVisible && window.SettingsTabs.IsVisible, "studio_png_settings_navigation_" + tab);
                check(Find<Button>(window.SettingsTabs).Where(b => b.Content is UIElement).All(b => b.ContentTemplate == null), "studio_settings_preserve_custom_button_content_" + tab);
                await ScreenEvidence.CaptureAsync(window, "studio-settings-" + tab, check);
            }
            check(inputCalls == 0, "studio_qa_never_starts_microphone");
        }
        finally { resume?.Invoke(); await window.ShutdownAsync(); window.Close(); Directory.Delete(folder, true); }
    }
}
