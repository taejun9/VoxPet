using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.Views;

/// <summary>가짜 입력의 숫자로 실제 DispatcherTimer·명령·측정/취소/종료 흐름을 검증한다.</summary>
internal static class UsabilityQa
{
    private sealed class NumericInput : IAudioInput
    {
        public event Action<AudioLevel>? LevelAvailable;
        public event Action<Exception?>? Ended;
        private System.Threading.Timer? timer;
        public bool Emit { get; set; } = true;
        public Task StartAsync()
        {
            if (Emit) timer = new(_ => LevelAvailable?.Invoke(new(.001, .002, -60)), null, 0, 20);
            return Task.CompletedTask;
        }
        public async Task StopAsync() { if (timer != null) await timer.DisposeAsync(); timer = null; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Fail() => Ended?.Invoke(new IOException());
    }
    private static async Task WaitAsync(Func<bool> done, double seconds = 5)
    {
        var clock = Stopwatch.StartNew();
        while (!done() && clock.Elapsed.TotalSeconds < seconds) await Task.Delay(20);
        if (!done()) throw new TimeoutException("Usability QA condition did not complete");
    }
    public static async Task RunAsync(Action<bool, string> check)
    {
        var input = new NumericInput();
        await using var model = new MainViewModel(false, () => [new AudioDevice("QA", "합성 마이크")], _ => input);
        await model.InitializeAsync();
        check(!model.MeasureNoiseCommand.CanExecute(null), "noise_measure_disabled_when_stopped");
        model.DemoCommand.Execute(null);
        check(!model.MeasureNoiseCommand.CanExecute(null), "noise_measure_disabled_in_demo");
        model.StopCommand.Execute(null); await WaitAsync(() => model.CanChooseDevice);
        model.StartCommand.Execute(null); await WaitAsync(() => model.MeasureNoiseCommand.CanExecute(null));
        string status = model.Status;
        double original = model.NoiseGate, min = model.NormalizeMin, max = model.NormalizeMax;
        model.MeasureNoiseCommand.Execute(null);
        check(model.IsMeasuringNoise && !model.MeasureNoiseCommand.CanExecute(null) && model.StopCommand.CanExecute(null), "noise_measure_running_allows_stop");
        await WaitAsync(() => !model.IsMeasuringNoise);
        check(model.ApplyNoiseCommand.CanExecute(null) && model.NoiseGate == original && model.Status == status, "noise_recommendation_requires_explicit_apply");
        check(model.CancelNoiseCommand.CanExecute(null), "noise_pending_recommendation_can_cancel");
        model.ApplyNoiseCommand.Execute(null);
        check(model.NoiseGate == -54 && model.NormalizeMin == min && model.NormalizeMax == max && !model.ApplyNoiseCommand.CanExecute(null), "noise_apply_changes_gate_only");
        model.MeasureNoiseCommand.Execute(null); await WaitAsync(() => !model.IsMeasuringNoise);
        model.CancelNoiseCommand.Execute(null);
        check(!model.ApplyNoiseCommand.CanExecute(null) && !model.CancelNoiseCommand.CanExecute(null) && model.NoiseGate == -54, "noise_cancel_discards_pending_recommendation");
        model.MeasureNoiseCommand.Execute(null); model.CancelNoiseCommand.Execute(null);
        check(!model.IsMeasuringNoise && !model.ApplyNoiseCommand.CanExecute(null) && model.NoiseGate == -54, "noise_cancel_preserves_settings");
        model.MeasureNoiseCommand.Execute(null); model.ConversationCommand.Execute(null);
        check(!model.IsMeasuringNoise && !model.ApplyNoiseCommand.CanExecute(null) && model.NoiseGate == -50, "noise_preset_invalidates_measurement");
        model.MeasureNoiseCommand.Execute(null); model.StopCommand.Execute(null); await WaitAsync(() => model.CanChooseDevice);
        check(!model.IsMeasuringNoise && !model.ApplyNoiseCommand.CanExecute(null), "noise_stop_cancels_measurement");
        input.Emit = false;
        model.StartCommand.Execute(null); await WaitAsync(() => model.MeasureNoiseCommand.CanExecute(null));
        model.MeasureNoiseCommand.Execute(null); await WaitAsync(() => !model.IsMeasuringNoise);
        check(!model.ApplyNoiseCommand.CanExecute(null) && model.NoiseGate == -50 && model.CalibrationStatus.Contains("만들지 못"), "noise_no_callbacks_preserves_settings");
        model.MeasureNoiseCommand.Execute(null); input.Fail(); await WaitAsync(() => model.CanChooseDevice);
        check(!model.IsMeasuringNoise && !model.ApplyNoiseCommand.CanExecute(null) && model.Status.Contains("중단"), "noise_device_failure_cancels_measurement");
        int opens = 0; model.OpenMicrophonePrivacy += () => opens++; model.MicrophonePrivacyCommand.Execute(null);
        check(opens == 1, "microphone_privacy_command_requests_settings");
        check(model.HandleMuteKey(Key.M, ModifierKeys.Control | ModifierKeys.Shift, false) && model.CharacterMuted, "mute_local_fallback_toggles");
        check(model.HandleMuteKey(Key.M, ModifierKeys.Control | ModifierKeys.Shift, true) && model.CharacterMuted, "mute_local_repeat_does_not_toggle");
        check(!model.HandleMuteKey(Key.M, ModifierKeys.Control, false) && model.CharacterMuted, "mute_wrong_modifiers_ignored");
        model.IsGlobalMuteKey = () => true;
        check(!model.HandleMuteKey(Key.M, ModifierKeys.Control | ModifierKeys.Shift, false) && model.CharacterMuted, "mute_global_key_not_processed_twice");
        model.StartCommand.Execute(null); await WaitAsync(() => model.MeasureNoiseCommand.CanExecute(null));
        model.MeasureNoiseCommand.Execute(null);
        await model.DisposeAsync();
        check(!model.IsMeasuringNoise && !model.MeasureNoiseCommand.CanExecute(null) && !model.MicrophonePrivacyCommand.CanExecute(null), "usability_shutdown_cancels_and_disables_commands");
    }
}
