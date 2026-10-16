using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Packages;

namespace VoxPet.App.Views;

/// <summary>worker 이미지의 문자열 변환·실제 검색/키보드/automation·보관함 재시작을 검사한다.</summary>
internal static class CardDisplayQa
{
    [DllImport("user32.dll")] private static extern uint GetDoubleClickTime();
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    private sealed class NonTextItem { public override string ToString() => throw new InvalidOperationException("Item must not be formatted"); }
    private static async Task WaitAsync(Func<bool> done)
    { for (int i = 0; i < 250 && !done(); i++) await Task.Delay(20); if (!done()) throw new InvalidOperationException("Card display QA operation did not complete"); }
    private static void RecordReproduction(InvalidOperationException error, Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-card-repro-" + Guid.NewGuid());
        try
        {
            string diagnostic = ErrorDiagnostics.Record(error, "card-display-repro", folder);
            string output = Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), "voxpet-runtime-card-display-repro.json");
            File.Copy(diagnostic, output, true);
            using var document = JsonDocument.Parse(File.ReadAllText(diagnostic));
            check(document.RootElement.GetProperty("errors")[0].GetProperty("methods").EnumerateArray().Any(m => m.GetString() == "VoxPet.App.ViewModels.CharacterCard.PrintMembers"), "card_display_original_printmembers_failure_reproduced");
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    public static async Task RunAsync(Action<bool, string> check)
    {
        var loaded = await Task.Run(() => CharacterPackageLibrary.Decode(new VoxPetPackageLoader().Load(StudioShellViewModel.SamplePath)));
        var image = loaded.Images[loaded.Package.Manifest.Thumbnail!];
        check(image.IsFrozen, "card_display_worker_thumbnail_frozen");
        var card = new CharacterCard("private-library", "QA 카드", "private.id", "1.0", "private-author", "private-description", image);
        bool safe = false;
        try { safe = card.ToString() == card.Name; }
        catch (InvalidOperationException error) { RecordReproduction(error, check); }
        check(safe, "card_display_worker_thumbnail_to_name_without_exception");
        if (!safe) return; // The failed contract is recorded before attempting native UI interactions.
        bool decoderRejectsUi = false;
        try { _ = image.ToString(); } catch (InvalidOperationException) { decoderRejectsUi = true; }
        check(decoderRejectsUi, "card_display_frozen_decoder_thread_boundary_fixture");
        var pixels = new byte[512 * 512 * 4]; ((BitmapSource)image).CopyPixels(pixels, 512 * 4, 0);
        check(pixels.Any(p => p != 0), "card_display_frozen_worker_image_still_drawable");
        var mutable = new DrawingImage(new GeometryDrawing(Brushes.Blue, null, new RectangleGeometry(new Rect(0, 0, 4, 4))));
        CharacterCard[] cards = [card, card with { Image = null, Name = "빈 그림" }, card with { Image = mutable, Name = "한글 · é · 🐾" }];
        foreach (var item in cards)
        {
            check(item.ToString() == item.Name && string.Format(CultureInfo.InvariantCulture, "{0}", item) == item.Name, "card_display_label_only_" + Array.IndexOf(cards, item));
            check(await Task.Run(() => item.ToString() == item.Name && item.GetHashCode() == (item with { }).GetHashCode()), "card_display_worker_format_and_identity_" + Array.IndexOf(cards, item));
            var text = new TextBlock(); text.SetBinding(TextBlock.TextProperty, new Binding { Source = item });
            check(text.Text == item.Name, "card_display_wpf_implicit_text_binding_" + Array.IndexOf(cards, item)); BindingOperations.ClearAllBindings(text);
        }
        var row = new EditorRow("row", "레이어 이름", new NonTextItem(), image);
        check(row.ToString() == row.Name && await Task.Run(() => row.ToString() == row.Name), "card_display_editor_row_never_formats_item_or_image");
        var layer = new RuntimeLayer(image, new(0, 0));
        check(layer.ToString() == nameof(RuntimeLayer) && await Task.Run(() => layer.ToString() == nameof(RuntimeLayer)), "card_display_runtime_layer_never_formats_image");
        check(await Task.Run(() => Enumerable.Range(0, 1000).AsParallel().All(i => cards[i % cards.Length].ToString() == cards[i % cards.Length].Name)), "card_display_parallel_label_reads");
        check(typeof(App).Assembly.GetName().Version?.ToString() == "1.1.0.0", "card_display_fixed_build_version");
        await CheckLibraryAsync(loaded.Package, check);
    }
    private static async Task CheckLibraryAsync(VoxPetPackage sample, Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-card-library-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        string characters = Path.Combine(folder, "Characters"); Directory.CreateDirectory(characters);
        foreach (var character in VoxPet.Core.Models.CharacterCatalog.All) DetailedCharacterQa.SaveFixture(Path.Combine(characters, character.FileName), 0);
        int inputs = 0;
        MainViewModel Model() => new(true, () => [], _ => { inputs++; throw new InvalidOperationException(); }, charactersFolder: characters, dataFolder: folder, initialSettings: new(new()));
        var model = Model(); var window = new MainWindow(true, SystemParameters.WorkArea, model); MainWindow? restarted = null;
        try
        {
            window.Show(); await window.Ready; window.ShowStudio(1); window.Width = Math.Min(1100, SystemParameters.WorkArea.Width - 32); window.Height = Math.Min(760, SystemParameters.WorkArea.Height - 32);
            string Make(string id, string name)
            {
                var definition = new VoxPet.Core.Character.CharacterDefinition([new("head", "head", "head.png", "root")], [new("root")]);
                var package = new VoxPetPackageValidator().Validate(new(id, name, "1.0", thumbnail: "head.png"), definition, new Dictionary<string, byte[]> { ["head.png"] = sample.ReadAsset("images/head.png") });
                string path = Path.Combine(folder, id + ".voxpet"); new VoxPetPackageExporter().Export(package, path); return path;
            }
            string alpha = Make("qa.alpha", "Alpha 카드"), beta = Make("qa.beta", "Beta 카드");
            check(await model.Packages.ImportAsync(alpha) && await model.Packages.ImportAsync(beta), "card_library_import_native_thumbnails");
            await window.Studio.RefreshAsync(); window.UpdateLayout();
            var studio = window.Studio; var list = window.StudioSurface.LibraryList;
            check(list.Items.Count == 4 && studio.Cards.All(c => c.Image is { IsFrozen: true } && c.ToString() == c.Name), "card_library_legacy_sample_native_labels_and_images");
            check(TextSearch.GetTextPath(list) == "Name" && list.IsTextSearchEnabled, "card_library_explicit_text_search_contract");
            foreach (var item in studio.Cards)
            {
                var container = (ListBoxItem)list.ItemContainerGenerator.ContainerFromItem(item); var peer = UIElementAutomationPeer.CreatePeerForElement(container)!;
                check(peer.GetName() == item.Name, "card_library_automation_name_" + item.Id);
            }
            list.SelectedIndex = list.Items.Count - 1; list.Focus(); window.Activate(); await Task.Delay(40);
            keybd_event(0x24, 0, 0, UIntPtr.Zero); keybd_event(0x24, 0, 2, UIntPtr.Zero); await Task.Delay(40);
            check(ReferenceEquals(list.SelectedItem, list.Items[0]) && ReferenceEquals(studio.Selected, list.SelectedItem), "card_library_native_home_selection");
            keybd_event(0x23, 0, 0, UIntPtr.Zero); keybd_event(0x23, 0, 2, UIntPtr.Zero); await Task.Delay(40);
            check(ReferenceEquals(list.SelectedItem, list.Items[^1]) && ReferenceEquals(studio.Selected, list.SelectedItem), "card_library_native_end_selection");
            void Type(string text) => list.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TextComposition(InputManager.Current, list, text)) { RoutedEvent = TextCompositionManager.TextInputEvent });
            list.SelectedItem = studio.Cards.Single(c => c.Id == "qa.alpha");
            Type("B"); await Task.Delay(30); check(studio.Selected?.Id == "qa.beta", "card_library_text_composition_search");
            await Task.Delay(checked((int)GetDoubleClickTime() * 2 + 100)); Type("늘"); await Task.Delay(30); check(studio.Selected?.Legacy?.Id == "neulbo", "card_library_korean_text_search");
            await ScreenEvidence.CaptureAsync(window, "card-library-keyboard", check);
            studio.Search = "Alpha"; window.UpdateLayout(); check(list.Items.Count == 1 && ((CharacterCard)list.Items[0]).Id == "qa.alpha", "card_library_filter_name");
            studio.Search = "qa.beta"; window.UpdateLayout(); check(list.Items.Count == 1 && ((CharacterCard)list.Items[0]).Id == "qa.beta", "card_library_filter_id");
            studio.Search = "unmatched"; window.UpdateLayout(); check(list.Items.Count == 0 && studio.Selected == null && !studio.SelectCommand.CanExecute(null) && !studio.EditCommand.CanExecute(null), "card_library_empty_filter_disables_selection");
            studio.Search = ""; window.UpdateLayout();
            for (int i = 0; i < 4; i++)
            {
                string id = i % 2 == 0 ? "qa.alpha" : "qa.beta";
                list.SelectedItem = studio.Cards.Single(c => c.Id == id); studio.SelectCommand.Execute(null);
                await WaitAsync(() => model.Packages.Loaded?.Package.Manifest.Id == id && !model.Packages.IsBusy);
                check(model.Character.Name == studio.Selected!.Name && ReferenceEquals(model.Character.RuntimeScene, model.Packages.Scene), "card_library_repeated_select_" + i);
                await studio.RefreshAsync(); window.UpdateLayout(); check(studio.Selected?.Id == id && list.SelectedItem is CharacterCard c && c.Id == id, "card_library_refresh_retains_selection_" + i);
            }
            var before = model.Packages.Scene; string bad = Path.Combine(folder, "bad.voxpet"); File.WriteAllText(bad, "bad");
            check(!await model.Packages.ImportAsync(bad) && ReferenceEquals(before, model.Packages.Scene), "card_library_corrupt_import_keeps_active_scene");
            list.SelectedItem = studio.Cards.Single(c => c.Id == "qa.alpha"); studio.EditCommand.Execute(null);
            await WaitAsync(() => studio.Page == 5 && studio.Editor.Document?.Package.Manifest.Id == "qa.alpha" && !studio.Editor.IsBusy);
            studio.Editor.PausePreview(); studio.Editor.Page = 1;
            check(studio.Editor.Rows.All(r => r.ToString() == r.Name), "card_library_editor_rows_after_card_open");
            await ScreenEvidence.CaptureAsync(window, "card-library-editor", check);
            string exported = Path.Combine(folder, "exported.voxpet"); check(await studio.Editor.ExportAsync(exported) && new VoxPetPackageLoader().Load(exported).Manifest.Id == "qa.alpha", "card_library_editor_export_reload");
            window.ShowStudio(1); await studio.RefreshAsync(); list.SelectedItem = studio.Cards.Single(c => c.Id == "qa.beta"); studio.DeleteCommand.Execute(null);
            await WaitAsync(() => !studio.Cards.Any(c => c.Id == "qa.beta"));
            check(File.Exists(beta) && !model.Packages.Entries.Any(e => e.CharacterId == "qa.beta"), "card_library_delete_preserves_original");
            list.SelectedItem = studio.Cards.Single(c => c.Id == "qa.alpha"); studio.SelectCommand.Execute(null); await WaitAsync(() => model.Packages.Loaded?.Package.Manifest.Id == "qa.alpha" && !model.Packages.IsBusy);
            await window.ShutdownAsync(); window.Close();
            var restored = Model(); restarted = new MainWindow(true, SystemParameters.WorkArea, restored); restarted.Show(); await restarted.Ready; await restarted.Studio.RefreshAsync(); restarted.ShowStudio(1); restarted.UpdateLayout();
            check(restored.Packages.Loaded?.Package.Manifest.Id == "qa.alpha" && restarted.Studio.Cards.Any(c => c.Id == "qa.alpha"), "card_library_restart_restores_native_selection");
            check(restarted.Studio.Cards.All(c => c.ToString() == c.Name), "card_library_restart_labels_safe");
            await ScreenEvidence.CaptureAsync(restarted, "card-library-restarted", check);
            check(inputs == 0, "card_library_never_opens_microphone");
        }
        finally
        {
            if (restarted != null) { await restarted.ShutdownAsync(); restarted.Close(); }
            await window.ShutdownAsync(); window.Close(); Directory.Delete(folder, true);
        }
    }
}
