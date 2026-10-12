using System.IO;
using System.Windows;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;

namespace VoxPet.App.Views;

/// <summary>임시 설정/가짜 목록으로 자동 저장과 실제 WPF 창 재실행을 검사한다. 마이크 입력은 생성하지 않는다.</summary>
internal static class SettingsPersistenceQa
{
    private static async Task WaitAsync(Func<bool> condition)
    {
        for (int i = 0; i < 250 && !condition(); i++) await Task.Delay(20);
        if (!condition()) throw new TimeoutException("Settings QA did not settle");
    }
    private static async Task CloseAsync(MainWindow window)
    {
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => closed.TrySetResult(); window.Close();
        await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }
    internal static async Task RunAsync(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-settings-qa-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        var store = new SettingsStore(Path.Combine(folder, "settings.json"));
        IReadOnlyList<AudioDevice> devices = [new("QA-a", "합성 A"), new("QA-b", "합성 B")];
        int inputs = 0;
        MainViewModel CreateModel() => new(true, () => devices, _ => { inputs++; throw new InvalidOperationException("QA must not start capture"); }, Path.Combine(folder, "Characters"), folder);
        MainWindow? window = null;
        try
        {
            check(store.Save(new(new(Sensitivity: 1.8), false, false, MicrophoneId: "QA-b", MainWindow: new(800, 560, 80, 60),
                BroadcastWindow: new(410, 270, 220, 190), BroadcastOpen: true, CharacterMuted: true, SettingsTab: 2, ExpressionSlotId: 7)), "settings_fixture_seed");
            var model = CreateModel(); window = new(true, SystemParameters.WorkArea, model);
            window.Show(); await window.Ready;
            var broadcast = window.ShowCharacter();
            await ScreenEvidence.CaptureAsync(window, "settings-initial", check);
            await ScreenEvidence.CaptureAsync(broadcast, "settings-initial-broadcast", check);
            check(model.SelectedDevice?.Id == "QA-b" && model.PreferredMicrophoneId == "QA-b" && inputs == 0 && model.VoiceLevel == 0, "settings_microphone_restored_without_capture");
            check(window.Width == 800 && window.Height == 560 && window.SettingsTabs.SelectedIndex == 2 && model.CharacterMuted && model.Expressions.Selected.Index == 7, "settings_main_ui_and_expression_restore");
            check(broadcast.Width == 410 && broadcast.Height == 270 && model.BroadcastOpen && !model.GreenBackground && !model.Topmost, "settings_broadcast_restored_open_and_size");
            window.Width = 820; window.Height = 590; window.Left = 120; window.Top = 100;
            broadcast.Left = 250; broadcast.Top = 210; model.BroadcastWidth = 530; model.BroadcastHeight = 360;
            model.SettingsTab = 4; model.Sensitivity = 2.1; model.GreenBackground = true; model.Topmost = true; model.CharacterMuted = false;
            model.SelectedDevice = model.Devices[0]; await model.Expressions.ActivateAsync(5);
            await WaitAsync(() => store.Load().Audio.Sensitivity == 2.1 && store.Load().SettingsTab == 4 && store.Load().ExpressionSlotId == 5);
            var written = store.Load(); var normal = model.MainPlacement!; var broadcastNormal = model.BroadcastPlacement!;
            check(written.MicrophoneId == "QA-a" && !written.CharacterMuted && written.Topmost && written.GreenBackground, "settings_live_autosave_last_controls");
            check(written.MainWindow == normal && written.BroadcastWindow == broadcastNormal && written.MainWindow?.Width == 820 && written.BroadcastWindow?.Width == 530, "settings_live_autosave_geometry");
            devices = [new("QA-b", "합성 B")]; model.RefreshCommand.Execute(null);
            await WaitAsync(() => model.CanChooseDevice && model.SelectedDevice?.Id == "QA-b"); await model.FlushSettingsAsync();
            check(model.PreferredMicrophoneId == "QA-a" && store.Load().MicrophoneId == "QA-a", "settings_missing_microphone_retains_preference");
            devices = [new("QA-b", "합성 B"), new("QA-a", "합성 A")]; model.RefreshCommand.Execute(null);
            await WaitAsync(() => model.CanChooseDevice && model.SelectedDevice?.Id == "QA-a");
            check(model.SelectedDevice?.Id == "QA-a" && inputs == 0, "settings_reconnected_microphone_restored_despite_order");
            window.WindowState = WindowState.Maximized; await Task.Delay(50);
            window.WindowState = WindowState.Minimized; window.HideToTray(); await model.FlushSettingsAsync();
            check(store.Load().MainWindow == (normal with { Maximized = true }), "settings_minimize_tray_preserves_normal_bounds_and_maximized");
            await CloseAsync(window); window = null;
            written = store.Load(); check(written.BroadcastOpen && written.MainWindow!.Maximized, "settings_exit_flush_preserves_open_broadcast");

            model = CreateModel(); window = new(true, SystemParameters.WorkArea, model); window.Show(); await window.Ready;
            broadcast = window.ShowCharacter();
            check(window.WindowState == WindowState.Maximized && model.MainPlacement == written.MainWindow && model.SelectedDevice?.Id == "QA-a", "settings_restart_maximized_position_and_microphone");
            check(model.BroadcastPlacement == written.BroadcastWindow && broadcast.Width == 530 && broadcast.Height == 360 && model.SettingsTab == 4 && model.Expressions.Selected.Index == 5, "settings_restart_broadcast_position_and_last_expression");
            await ScreenEvidence.CaptureAsync(window, "settings-restarted", check);
            await ScreenEvidence.CaptureAsync(broadcast, "settings-restarted-broadcast", check);
            model.BroadcastWidth = 510; await Task.Delay(50);
            check(broadcast.Width == 510 && model.BroadcastPlacement!.Width == 510, "settings_restored_broadcast_preserves_two_way_size_binding");
            broadcast.Close(); await model.FlushSettingsAsync();
            check(!store.Load().BroadcastOpen, "settings_manual_broadcast_close_is_saved");
            window.WindowState = WindowState.Normal; await model.Expressions.PreviewBuiltinAsync(2); await CloseAsync(window); window = null;

            model = CreateModel(); await model.InitializeAsync();
            try
            {
                check(!model.BroadcastOpen && model.SnapshotSettings().BuiltinExpression == ExpressionKind.Sad && model.SnapshotSettings().ExpressionSlotId == null && inputs == 0, "settings_builtin_face_restored_without_saved_slot_or_capture");
                await model.Expressions.TestAllAsync(TimeSpan.FromMilliseconds(1));
                check(model.SnapshotSettings().BuiltinExpression == ExpressionKind.Sad, "settings_temporary_expression_test_does_not_replace_saved_selection");
                string before = File.ReadAllText(Path.Combine(folder, "settings.json"));
                Directory.CreateDirectory(Path.Combine(folder, "settings.json.tmp")); model.Sensitivity = 3;
                check(!await model.FlushSettingsAsync() && model.SettingsStatus.Length > 0 && File.ReadAllText(Path.Combine(folder, "settings.json")) == before, "settings_write_failure_keeps_old_file_and_reports");
                Directory.Delete(Path.Combine(folder, "settings.json.tmp"));
                check(await model.FlushSettingsAsync() && model.SettingsStatus == "" && store.Load().Audio.Sensitivity == 3, "settings_write_failure_retry_recovers");
                devices = []; model.RefreshCommand.Execute(null); await WaitAsync(() => model.CanChooseDevice && model.SelectedDevice == null);
                check(model.PreferredMicrophoneId == "QA-a" && inputs == 0, "settings_empty_microphone_list_keeps_preference");
            }
            finally { await model.DisposeAsync(); }
        }
        finally
        {
            if (window != null) await CloseAsync(window);
            Directory.Delete(folder, true);
        }
    }
}
