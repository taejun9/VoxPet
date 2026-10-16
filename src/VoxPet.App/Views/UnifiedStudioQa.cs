using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.ViewModels;
using VoxPet.Core.Attachments;
using VoxPet.Core.Editing;
using VoxPet.Core.Packages;

namespace VoxPet.App.Views;

internal static class UnifiedStudioQa
{
    private static IEnumerable<T> Find<T>(DependencyObject node) where T : DependencyObject
    { if (node is T item) yield return item; for (int i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++) foreach (var child in Find<T>(VisualTreeHelper.GetChild(node, i))) yield return child; }
    internal static string Render(CharacterViewModel character)
    {
        var surface = new MotionSurface { DataContext = character }; surface.Measure(new Size(512, 512)); surface.Arrange(new Rect(0, 0, 512, 512)); surface.UpdateLayout();
        var image = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32); image.Render(surface);
        var pixels = new byte[512 * 512 * 4]; image.CopyPixels(pixels, 2048, 0); return Convert.ToHexString(SHA256.HashData(pixels));
    }
    internal static async Task CheckSelectorAsync(ComboBox combo, string id, Action<bool, string> check)
    {
        combo.UpdateLayout();
        var selected = Find<TextBlock>(combo).Where(t => t.IsVisible && !string.IsNullOrWhiteSpace(t.Text)).ToArray();
        check(selected.Length > 0 && selected.All(t => t.Foreground is SolidColorBrush b && b.Color == Colors.Black), "studio_selector_black_" + id);
        combo.IsDropDownOpen = true; await Task.Delay(30); combo.UpdateLayout();
        var item = combo.ItemContainerGenerator.ContainerFromIndex(0) as ComboBoxItem;
        check(item != null && Find<TextBlock>(item).Any(t => t.IsVisible && t.Foreground is SolidColorBrush b && b.Color == Colors.Black), "studio_selector_popup_black_" + id);
        combo.IsDropDownOpen = false;
    }
    // Preserve the original native-package/editor regressions using their public fixture.
    // Product sample navigation is independently tested on both real catalog selections.
    private static async Task UseReferenceSamplesAsync(MainWindow window)
    {
        var model = window.Model; int destination = window.Studio.Page == 3 ? 3 : 2;
        await model.Packages.RefreshAsync();
        var current = model.Packages.Entries.FirstOrDefault(e => e.CharacterId == StudioShellViewModel.SampleId && e.Version == StudioShellViewModel.SampleVersion);
        if (current == null) await model.Packages.ImportAsync(StudioShellViewModel.SamplePath); else await model.Packages.SelectAsync(current.LibraryId);
        window.Studio.Page = destination; await window.Studio.RefreshAsync();
    }
    public static async Task RunAsync(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-studio-qa-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        int inputCalls = 0;
        var model = new MainViewModel(true, () => [], _ => { inputCalls++; throw new InvalidOperationException(); }, dataFolder: folder, charactersFolder: Path.Combine(folder, "missing"), initialSettings: new(new()));
        var window = new MainWindow(true, SystemParameters.WorkArea, model); Action? resume = null;
        try
        {
            window.Show(); await window.Ready; model.Devices.Add(new VoxPet.App.Services.AudioDevice("qa-studio", "QA 마이크 표시 시험")); model.SelectedDevice = model.Devices[0]; resume = model.PauseVisualForQa(); window.ShowStudio();
            check(window.StudioSurface.IsVisible && !window.LayoutRoot.IsVisible && ReferenceEquals(window.SettingsTabs.Parent, window.StudioSurface.SettingsHost), "studio_single_shell_mounts_settings");
            await window.Studio.RefreshAsync(); check(window.Studio.Cards.All(c => !c.Sample), "studio_no_standalone_sample_card");
            var bundled = new VoxPetPackageLoader().Load(StudioShellViewModel.SamplePath);
            var m = bundled.Manifest;
            string previous = Path.Combine(folder, "previous.voxpet");
            File.WriteAllBytes(previous, new CharacterDocument(bundled).Metadata(m.Id, m.Name, "1.0.0", m.Author, m.Description, m.Thumbnail, 512, 512).Export());
            check(await model.Packages.ImportAsync(previous), "studio_previous_sample_fixture");
            string previousId = model.Packages.Loaded!.LibraryId;
            string managedPrevious = Path.Combine(folder, "CharactersV2", previousId + ".voxpet");
            byte[] previousBytes = File.ReadAllBytes(managedPrevious);
            await UseReferenceSamplesAsync(window);
            check(model.Packages.Loaded?.Package.Manifest.Version == StudioShellViewModel.SampleVersion && model.Packages.IsActive && window.Studio.AttachmentCards.Count == 17 && window.Studio.LiveAttachments.Select(a => a.Category).Distinct().Order().SequenceEqual(AttachmentSystem.Categories.Order()) && window.Studio.LiveActions.Count == 9, "studio_sample_ready_without_user_files");
            var scene = model.Packages.Scene!; double now = scene.Runtime.Actions.LastTime;
            await UseReferenceSamplesAsync(window); check(model.Packages.Entries.Count(e => e.CharacterId == StudioShellViewModel.SampleId && e.Version == StudioShellViewModel.SampleVersion) == 1, "studio_sample_reselect_reuses_managed_copy");
            check(model.Packages.Entries.Any(e => e.LibraryId == previousId) && previousBytes.SequenceEqual(File.ReadAllBytes(managedPrevious)), "studio_sample_upgrade_preserves_previous_copy");
            scene = model.Packages.Scene!; now = scene.Runtime.Actions.LastTime;
            window.Width = Math.Min(1280, SystemParameters.WorkArea.Width - 40); window.Height = Math.Min(820, SystemParameters.WorkArea.Height - 40); window.Left = 20; window.Top = 20;
            foreach (var card in window.Studio.AttachmentCards)
            {
                card.ToggleCommand.Execute(null); model.Packages.Update(now += .1, 0);
                check(card.IsWorn && card.WearLabel == "해제" && scene.Runtime.Definition.Attachments.Single(a => a.Id == card.Id).Layers.All(l => scene.Frame.Layers.Any(rendered => rendered.Asset == (l.Animation?.FrameAt(now) ?? l.Asset))), "studio_wear_" + card.Id);
                Render(model.Character);
                await ScreenEvidence.CaptureAsync(window, "studio-costume-" + card.Id.ToLowerInvariant(), check);
                card.ToggleCommand.Execute(null); check(!card.IsWorn, "studio_remove_" + card.Id);
            }
            now = await AnimatedAccessoriesQa.RunAsync(window, model, folder, now, check);
            var top = window.Studio.AttachmentCards.Single(c => c.Id == "top"); top.ToggleCommand.Execute(null);
            var suit = window.Studio.AttachmentCards.Single(c => c.Id == "onePiece"); suit.ToggleCommand.Execute(null);
            check(suit.IsWorn && !top.IsWorn && top.WearLabel == "착용", "studio_costume_conflicts_refresh_all_cards");
            window.Studio.RemoveAttachmentsCommand.Execute(null); top.ToggleCommand.Execute(null);
            window.Studio.Page = 3; window.UpdateLayout();
            var sampleNames = window.Studio.LiveAttachments.Select(a => a.Name).Concat(window.Studio.LiveActions.Select(a => a.Name)).ToHashSet();
            var sampleTexts = Find<TextBlock>(window.StudioSurface).Where(t => sampleNames.Contains(t.Text)).ToArray();
            bool contrast = sampleTexts.All(t => t.Foreground is SolidColorBrush b && b.Color.R < 128 && b.Color.G < 128 && b.Color.B < 128);
            if (!contrast) File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), "voxpet-runtime-studio-contrast.json"), System.Text.Json.JsonSerializer.Serialize(sampleTexts.Select(t => new { t.Text, t.IsVisible, model = t.DataContext?.GetType().Name, color = t.Foreground is SolidColorBrush b ? b.Color.ToString() : "not-solid" })));
            check(contrast, "studio_sample_card_text_contrast");
            foreach (var action in window.Studio.LiveActions)
            {
                model.Packages.Update(now += 2, 0); string rest = Render(model.Character);
                var button = Find<Button>(window.StudioSurface).Single(b => b.Tag is string id && id == action.Id); button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                model.Packages.Update(now, 0); model.Packages.Update(now += .2, 0);
                check(scene.Runtime.State.ActionId == action.Id && Render(model.Character) != rest && scene.Frame.Layers.Any(l => l.Mesh != null), "studio_action_with_costume_" + action.Id);
                await ScreenEvidence.CaptureAsync(window, "studio-action-" + action.Id, check);
                model.Packages.Update(now += 1.2, 0); check(scene.Runtime.State.ActionId == null, "studio_action_idle_" + action.Id);
            }
            window.Studio.Page = 3; await UseReferenceSamplesAsync(window); check(window.Studio.Page == 3, "studio_action_sample_keeps_page");
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
                    if (page == 0) await CheckSelectorAsync(Find<ComboBox>(window.StudioSurface).Single(c => c.IsVisible && ReferenceEquals(c.ItemsSource, model.Devices)), $"microphone_{size.Width}", check);
                    if (page == 4)
                    {
                        await CheckSelectorAsync(window.StudioSurface.SettingsSection, $"settings_{size.Width}", check);
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
                await CheckSelectorAsync(window.StudioSurface.SettingsSection, "png_settings_" + tab, check);
                check(!window.LayoutRoot.IsVisible && window.SettingsTabs.IsVisible, "studio_png_settings_navigation_" + tab);
                if (tab == 4) check(window.StudioSurface.SettingsSection.SelectedItem is string label && label.Contains("눈물") && Find<CheckBox>(window.SettingsTabs).Any(c => c.IsVisible && c.Content is string text && text == "눈 깜빡임"), "studio_settings_label_matches_blink_tears_panel");
                check(Find<Button>(window.SettingsTabs).Where(b => b.Content is UIElement).All(b => b.ContentTemplate == null), "studio_settings_preserve_custom_button_content_" + tab);
                await ScreenEvidence.CaptureAsync(window, "studio-settings-" + tab, check);
            }
            check(inputCalls == 0, "studio_qa_never_starts_microphone");
        }
        finally { resume?.Invoke(); await window.ShutdownAsync(); window.Close(); Directory.Delete(folder, true); }
    }
}
