using System.IO;
using System.Windows;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.Views;

/// <summary>합성 제작 틀로 로컬 등록·공유 렌더·손상 복구를 검사한다. 개인 PNG와 마이크는 사용하지 않는다.</summary>
internal static class ImageRuntimeQa
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    private static async Task CheckNativeInputsAsync(MainWindow window, Action<bool, string> check)
    {
        var target = new System.Windows.Controls.TextBox();
        var other = new Window { Width = 240, Height = 120, Title = "VoxPet synthetic input target", Content = target };
        var source = System.Windows.Interop.HwndSource.FromHwnd(new System.Windows.Interop.WindowInteropHelper(window).Handle);
        int hits = 0;
        using var input = new ImageRuntimeInputs(source, _ => hits++);
        try
        {
            input.Configure([new(RuntimeOperation.Play, 0x51, 0)], false, true);
            check(input.KeyboardRegistered, "imageset_native_keyboard_registered");
            other.Show(); other.Activate(); target.Focus(); await Task.Delay(50);
            keybd_event(0x51, 0, 0, UIntPtr.Zero); await Task.Delay(50);
            input.Configure([new(RuntimeOperation.Play, 0x51, 0)], false, true);
            keybd_event(0x51, 0, 0, UIntPtr.Zero); keybd_event(0x51, 0, 2, UIntPtr.Zero); await Task.Delay(50);
            check(hits == 1 && target.Text.Contains("q", StringComparison.OrdinalIgnoreCase), "imageset_background_key_passes_to_target_no_repeat");
            input.Dispose();
            keybd_event(0x51, 0, 0, UIntPtr.Zero); keybd_event(0x51, 0, 2, UIntPtr.Zero); await Task.Delay(50);
            check(hits == 1, "imageset_native_dispose_stops_input");
        }
        finally { other.Close(); window.Activate(); }
    }
    public static async Task RunAsync(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-image-qa-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        var model = new MainViewModel(false, () => [], _ => throw new InvalidOperationException(), dataFolder: folder);
        var window = new MainWindow(true, new Rect(0, 0, 1280, 820), model);
        try
        {
            window.Show(); await window.Ready;
            string template = Path.Combine(AppContext.BaseDirectory, "Assets", "ImageSetTemplate", "imageset.json");
            check(await model.Runtime.ImportAsync(template), "imageset_import_synthetic_template");
            model.Runtime.OutfitIndex = 1; model.Runtime.WeaponIndex = 1; model.Runtime.PlayCommand.Execute(null);
            model.Runtime.Update(0, 1, true, ExpressionKind.Happy);
            check(model.Runtime.IsActive && model.Character.RuntimeLayers is { Count: > 6 } layers && layers.All(l => l.Image.IsFrozen), "imageset_frozen_shared_layers");
            check(model.Character.Name.StartsWith("늘보군"), "imageset_character_name");
            window.SettingsTabs.SelectedIndex = 6; window.UpdateLayout();
            await ScreenEvidence.CaptureAsync(window, "imageset-preview", check, new { syntheticImageSet = true });
            var broadcast = window.ShowCharacter();
            await ScreenEvidence.CaptureAsync(broadcast, "imageset-broadcast", check, new { syntheticImageSet = true });
            check(ReferenceEquals(broadcast.DataContext, model), "imageset_shared_broadcast");
            var resume = model.PauseVisualForQa();
            var before = model.Character.RuntimeLayers!.Select(l => l.Image).ToArray();
            string bad = Path.Combine(folder, "bad.json"); File.WriteAllText(bad, "{}");
            check(!await model.Runtime.ImportAsync(bad) && model.Runtime.IsActive && model.Character.RuntimeLayers != null && before.SequenceEqual(model.Character.RuntimeLayers.Select(l => l.Image)), "imageset_invalid_import_keeps_live_set");
            resume();
            await CheckNativeInputsAsync(window, check);
            model.Runtime.Record(RuntimeOperation.Play);
            model.Runtime.Capture(System.Windows.Input.Key.Q, System.Windows.Input.ModifierKeys.None);
            check(model.Runtime.Bindings.Single().Key == 0x51, "imageset_key_capture");
            model.Runtime.Record(RuntimeOperation.Play); model.Runtime.Capture(System.Windows.Input.Key.Delete, System.Windows.Input.ModifierKeys.None);
            check(model.Runtime.Bindings.Length == 0, "imageset_key_clear");
            model.Runtime.ResetCommand.Execute(null); model.Runtime.Update(1, 0, false, ExpressionKind.Neutral);
            check(model.Runtime.OutfitIndex == 0 && model.Runtime.WeaponIndex == 0, "imageset_reset_outfit_weapon");
            string packId = Path.GetFileName(Directory.GetDirectories(Path.Combine(folder, "ImageSets")).Single());
            var preferences = new RuntimePreferences(new() { ["neulbo"] = new(packId, 0, 0, 0) }, [new(RuntimeOperation.Play, 0x51, 0)], true);
            check(new RuntimePreferencesStore(Path.Combine(folder, "image-runtime.json")).Save(preferences), "imageset_restart_fixture_saved");
            await window.ShutdownAsync(); window.Close();
            var restored = new MainViewModel(true, () => [], _ => throw new InvalidOperationException(), charactersFolder: Path.Combine(folder, "missing-characters"), dataFolder: folder, initialSettings: new(new()));
            var restoredWindow = new MainWindow(true, new Rect(0, 0, 1000, 730), restored);
            try
            {
                restoredWindow.Show(); await restoredWindow.Ready;
                check(restored.Runtime.IsActive && restored.Character.Name.StartsWith("늘보군") && restored.Runtime.OutfitIndex == 1 && restored.Runtime.WeaponIndex == 1 && restored.Runtime.LeftClick && restored.Runtime.Bindings.Single().Key == 0x51, "imageset_restart_without_legacy_png_restores_selections");
            }
            finally { await restoredWindow.ShutdownAsync(); restoredWindow.Close(); }

        }
        finally
        {
            await window.ShutdownAsync(); window.Close();
            Directory.Delete(folder, true);
        }
    }
}
