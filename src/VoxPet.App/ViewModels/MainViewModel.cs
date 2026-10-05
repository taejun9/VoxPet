using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Threading;
using VoxPet.App.Services;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IAsyncDisposable
{
    private readonly bool persistSettings;
    private readonly AudioSession session = new();
    private readonly AudioLevelProcessor processor = new();
    private readonly CharacterAnimator animator = new();
    private readonly SettingsStore store = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly DispatcherTimer timer;
    private AudioSettings settings;
    private AudioDevice? selected;
    private bool busy, demo, closing, topmost, green, muted, characterBusy;
    private string status = "마이크를 선택하고 Start를 누르세요.";
    private string metrics = "RMS 0.0000   Peak 0.0000   -120.0 dBFS";
    private double level, raw;
    private double previousTime;
    public ObservableCollection<AudioDevice> Devices { get; } = [];
    public CharacterViewModel Character { get; } = new();
    public AsyncCommand StartCommand { get; }
    public AsyncCommand StopCommand { get; }
    public AsyncCommand RefreshCommand { get; }
    public RelayCommand ResetCommand { get; }
    public RelayCommand DemoCommand { get; }
    public RelayCommand BroadcastCommand { get; }
    public AsyncCommand ImportCharacterCommand { get; }
    public RelayCommand DefaultCharacterCommand { get; }
    public RelayCommand ConversationCommand { get; }
    public RelayCommand SoftVoiceCommand { get; }
    public RelayCommand SnappyCommand { get; }
    public event Func<string?>? ChooseCharacterSheet;
    public event Action? OpenBroadcast;

    public MainViewModel(bool persistSettings = true)
    {
        this.persistSettings = persistSettings;
        var saved = persistSettings ? store.Load() : new UserSettings(new()); settings = saved.Audio; topmost = saved.Topmost; green = saved.GreenBackground;
        StartCommand = new(StartAsync, () => CanChooseDevice && SelectedDevice != null);
        StopCommand = new(StopAsync, () => !busy && !closing && (session.HasResources || demo));
        RefreshCommand = new(RefreshDevicesAsync, () => CanChooseDevice);
        ResetCommand = new(Reset);
        DemoCommand = new(ToggleDemo, () => CanChooseDevice);
        BroadcastCommand = new(() => OpenBroadcast?.Invoke());
        ImportCharacterCommand = new(async () =>
        {
            var path = ChooseCharacterSheet?.Invoke();
            if (path != null) await ImportCharacterAsync(path);
        }, () => !closing && !characterBusy);
        DefaultCharacterCommand = new(() => Character.RestoreDefault(), () => !closing && !characterBusy);
        ConversationCommand = new(() => ApplyPreset(ReactionPreset.Conversation));
        SoftVoiceCommand = new(() => ApplyPreset(ReactionPreset.SoftVoice));
        SnappyCommand = new(() => ApplyPreset(ReactionPreset.Snappy));
        timer = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(1000.0 / 60) };
        timer.Tick += Tick; timer.Start();
    }
    public AudioDevice? SelectedDevice { get => selected; set { if (Set(ref selected, value)) RefreshCommands(); } }
    public bool CanChooseDevice => !busy && !closing && !session.HasResources && !demo;
    public string Status { get => status; private set => Set(ref status, value); }
    public string Metrics { get => metrics; private set => Set(ref metrics, value); }
    public double VoiceLevel { get => level; private set => Set(ref level, value); }
    public double RawLevel { get => raw; private set => Set(ref raw, value); }
    public bool CharacterMuted
    {
        get => muted;
        set
        {
            if (!Set(ref muted, value)) return;
            Notify(nameof(MuteHint)); processor.Reset(); VoiceLevel = 0;
            Character.Update(animator.Update(0, clock.Elapsed.TotalSeconds));
        }
    }
    public string MuteHint => CharacterMuted ? "입 반응을 잠시 멈췄습니다. 마이크 해제는 Stop을 누르세요." : "음소거는 캐릭터 입만 멈춥니다. 마이크 입력은 계속 표시됩니다.";
    public bool Topmost { get => topmost; set => Set(ref topmost, value); }
    public bool GreenBackground { get => green; set { if (Set(ref green, value)) Notify(nameof(BroadcastBackground)); } }
    public Brush BroadcastBackground => GreenBackground ? Brushes.Lime : Brushes.Transparent;
    public string DemoLabel => demo ? "데모 종료 (Stop)" : "마이크 없이 데모";
    public double NoiseGate { get => settings.NoiseGate; set { settings = settings with { NoiseGate = value }; Notify(); } }
    public double NormalizeMin
    {
        get => settings.NormalizeMin;
        set
        {
            double min = Math.Clamp(value, -120, -1);
            settings = settings with { NormalizeMin = min, NormalizeMax = Math.Max(settings.NormalizeMax, min + 1) };
            Notify(); Notify(nameof(NormalizeMax));
        }
    }
    public double NormalizeMax
    {
        get => settings.NormalizeMax;
        set
        {
            double max = Math.Clamp(value, -119, 0);
            settings = settings with { NormalizeMax = max, NormalizeMin = Math.Min(settings.NormalizeMin, max - 1) };
            Notify(); Notify(nameof(NormalizeMin));
        }
    }
    public double Sensitivity { get => settings.Sensitivity; set { settings = settings with { Sensitivity = value }; Notify(); } }
    public double AttackMs { get => settings.AttackMs; set { settings = settings with { AttackMs = value }; Notify(); } }
    public double ReleaseMs { get => settings.ReleaseMs; set { settings = settings with { ReleaseMs = value }; Notify(); } }
    private void RefreshCommands()
    {
        Notify(nameof(CanChooseDevice)); Notify(nameof(DemoLabel));
        StartCommand.Refresh(); StopCommand.Refresh(); RefreshCommand.Refresh(); DemoCommand.Refresh();
        ImportCharacterCommand.Refresh(); DefaultCharacterCommand.Refresh();
    }
    public async Task InitializeAsync() => await RefreshDevicesAsync();
    private async Task RefreshDevicesAsync()
    {
        busy = true; RefreshCommands();
        try
        {
            string? old = SelectedDevice?.Id;
            var devices = await Task.Run(AudioCaptureService.ListDevices);
            Devices.Clear(); foreach (var device in devices) Devices.Add(device);
            SelectedDevice = Devices.FirstOrDefault(d => d.Id == old) ?? Devices.FirstOrDefault();
            Status = Devices.Count == 0 ? "사용 가능한 마이크가 없습니다. 장치를 연결한 뒤 새로고침하세요." : "마이크 준비 완료. Start를 누르면 로컬 분석을 시작합니다.";
        }
        catch { Status = "마이크 목록을 읽을 수 없습니다. Windows 마이크 권한과 연결을 확인하세요."; }
        finally { busy = false; RefreshCommands(); }
    }
    private async Task StartAsync()
    {
        var device = SelectedDevice;
        if (device == null || closing) return;
        busy = true; Status = "마이크 시작 중…"; RefreshCommands(); ResetLevels();
        try
        {
            await session.StartAsync(() => new AudioCaptureService(device.Id));
            Status = session.State == CaptureState.Running ? $"분석 중 · {device.Name} · 오디오는 저장/전송되지 않습니다." : session.Error ?? "마이크 시작 실패";
        }
        finally { busy = false; RefreshCommands(); }
    }
    private async Task StopAsync()
    {
        busy = true; demo = false; RefreshCommands(); Status = "마이크 종료 중…";
        try { await session.StopAsync(); ResetLevels(); Status = session.Error ?? "중지됨 · 마이크 캡처가 해제되었습니다."; }
        finally { busy = false; RefreshCommands(); }
    }
    private void ToggleDemo()
    {
        demo = true; ResetLevels(); RefreshCommands(); Status = "합성 데모 · 마이크를 사용하지 않습니다. Stop으로 종료하세요.";
    }
    public async Task<bool> ImportCharacterAsync(string path)
    {
        if (closing || characterBusy) return false;
        characterBusy = true; RefreshCommands();
        try
        {
            var sheet = await Task.Run(() => CharacterSheetLoader.Load(path));
            if (closing) return false;
            Character.UseSheet(sheet, Path.GetFileNameWithoutExtension(path));
            Status = "캐릭터 적용 완료 · 미리보기와 방송창에 함께 적용됩니다.";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or FormatException or OverflowException or COMException)
        {
            if (!closing) Status = "시트를 불러오지 못했습니다. 16MB 이하 RGBA PNG와 3열×2행 크기를 확인하세요. 기존 캐릭터를 유지합니다.";
            return false;
        }
        finally { characterBusy = false; RefreshCommands(); }
    }
    private void ApplyPreset(ReactionPreset preset)
    {
        settings = ReactionPresets.Create(preset);
        NotifyAudioSettings();
    }
    private void Reset() => ApplyPreset(ReactionPreset.Conversation);
    private void NotifyAudioSettings()
    {
        foreach (var property in new[] { nameof(NoiseGate), nameof(Sensitivity), nameof(AttackMs), nameof(ReleaseMs), nameof(NormalizeMin), nameof(NormalizeMax) }) Notify(property);
    }
    private void ResetLevels()
    {
        processor.Reset(); VoiceLevel = 0; RawLevel = 0; Metrics = "RMS 0.0000   Peak 0.0000   -120.0 dBFS";
        previousTime = clock.Elapsed.TotalSeconds;
        Character.Update(animator.Update(0, previousTime));
    }
    private async void Tick(object? sender, EventArgs e)
    {
        double now = clock.Elapsed.TotalSeconds;
        double dt = Math.Max(0, now - previousTime); previousTime = now;
        if (session.HasInputEnded && !busy && !closing) { await StopAsync(); return; }
        bool running = session.State == CaptureState.Running;
        var measured = session.ReadLevel(Stopwatch.GetTimestamp());
        if (demo)
        {
            double db = Math.Sin(now * 2.8) > -.25 ? -35 + 22 * Math.Max(0, Math.Sin(now * 6)) : -120;
            double rms = db <= -120 ? 0 : Math.Pow(10, db / 20);
            measured = new(rms, rms, db);
        }
        var result = processor.Update(measured.Dbfs, dt, settings);
        if ((!running && !demo) || CharacterMuted) processor.Reset();
        VoiceLevel = processor.VoiceLevel; RawLevel = running || demo ? result.Raw : 0;
        Metrics = $"RMS {measured.Rms:F4}   Peak {measured.Peak:F4}   {measured.Dbfs:F1} dBFS{(measured.Peak >= 1 ? " · CLIP" : "")}";
        Character.Update(animator.Update(VoiceLevel, now));
    }
    public async ValueTask DisposeAsync()
    {
        closing = true; timer.Stop(); timer.Tick -= Tick; RefreshCommands();
        // Queue behind any in-flight Start/Stop; no synchronous UI waits.
        await session.DisposeAsync(); ResetLevels();
        bool saved = !persistSettings || await Task.Run(() => store.Save(new(settings, Topmost, GreenBackground)));
        if (!saved) Status = "설정을 저장하지 못했습니다. 다음 실행에서 기본값을 사용합니다.";
    }
}
