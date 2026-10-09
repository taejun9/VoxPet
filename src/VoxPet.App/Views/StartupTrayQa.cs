using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using VoxPet.App.Services;

namespace VoxPet.App.Views;

internal static class StartupTrayQa
{
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr window);
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i); yield return child;
            foreach (var item in Descendants(child)) yield return item;
        }
    }
    internal static async Task RunAsync(Action<bool, string> check)
    {
        var window = new MainWindow(smoke: true); bool closed = false; window.Closed += (_, _) => closed = true;
        window.Show(); await window.Ready;
        try
        {
            window.SettingsTabs.SelectedIndex = 3; await Task.Delay(30); window.UpdateLayout();
            var pages = Descendants(window.Content as DependencyObject ?? window).OfType<TabControl>().First(control => !ReferenceEquals(control, window.SettingsTabs));
            pages.SelectedIndex = 1; await Task.Delay(30); window.UpdateLayout();
            var selector = Descendants(window).OfType<ComboBox>().First(combo => ReferenceEquals(combo.ItemsSource, window.Model.Expressions.Slots));
            var selected = window.Model.Expressions.Selected;
            selector.SelectedIndex = -1; await Task.Delay(20);
            check(ReferenceEquals(selected, window.Model.Expressions.Selected) && window.Model.Expressions.MoveDownCommand.CanExecute(null), "startup_selector_null_retains_valid_selection");
            await window.Model.Expressions.MoveAsync(1); await Task.Delay(30);
            check(ReferenceEquals(selected, selector.SelectedItem) && selected.Position == 1 && window.Model.Expressions.Slots[1].Index == selected.Index, "startup_bound_selector_reorder_preserves_selection");
            await window.Model.Expressions.MoveAsync(-1);
            var broadcast = window.ShowCharacter(); var firstHandle = new WindowInteropHelper(broadcast).Handle;
            check(!broadcast.AllowsTransparency && (GetWindowLongPtr(firstHandle, -20).ToInt64() & 0x80000) == 0, "obs_green_opaque_nonlayered_window");
            broadcast.Left = 120; broadcast.Top = 140; window.Model.BroadcastWidth = 600; window.Model.BroadcastHeight = 360;
            window.Model.GreenBackground = false; var transparent = window.ShowCharacter();
            check(!broadcast.IsVisible && transparent.AllowsTransparency && transparent.Left == 120 && transparent.Top == 140 && transparent.Width == 600 && transparent.Height == 360, "obs_background_mode_recreates_preserving_geometry");
            window.Model.DemoCommand.Execute(null); window.Model.TrayCommand.Execute(null);
            var broadcastHandle = new WindowInteropHelper(transparent).Handle;
            check(window.IsInTray && window.TrayIconVisible && !window.IsVisible && IsWindowVisible(broadcastHandle) && !IsIconic(broadcastHandle), "tray_hides_only_settings_keeps_broadcast");
            bool reacted = false;
            for (int i = 0; i < 80; i++) { await Task.Delay(20); reacted |= window.Model.VoiceLevel > .2; }
            check(reacted, "tray_keeps_synthetic_animation_running");
            window.ActivateTrayMenu(1); var compatible = window.ShowCharacter();
            check(!window.IsVisible && compatible.IsVisible && !compatible.AllowsTransparency && window.Model.GreenBackground, "tray_menu_opens_obs_compatible_broadcast");
            window.ActivateTrayMenu(0);
            check(window.IsVisible && window.WindowState == WindowState.Normal && !window.IsInTray && !window.TrayIconVisible, "tray_menu_restores_settings");
            window.WindowState = WindowState.Minimized; window.HideToTray(); window.RestoreFromTray();
            check(window.IsVisible && window.WindowState == WindowState.Normal && !window.TrayIconVisible, "tray_restores_previously_minimized_settings");
            window.HideToTray(); window.ActivateTrayMenu(3);
            for (int i = 0; i < 100 && !closed; i++) await Task.Delay(20);
            check(closed && !compatible.IsVisible && !window.TrayIconVisible && window.Model.VoiceLevel == 0, "tray_exit_hidden_window_closes_and_releases");
        }
        finally { if (!closed) window.Close(); }
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-diagnostic-qa-" + Guid.NewGuid());
        try
        {
            string path = ErrorDiagnostics.Record(new InvalidOperationException("PRIVATE-path-and-device"), "qa", folder);
            string text = File.ReadAllText(path);
            check(text.Contains("InvalidOperationException") && !text.Contains("PRIVATE-path-and-device") && !text.Contains(folder), "diagnostic_excludes_exception_message_and_private_paths");
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
