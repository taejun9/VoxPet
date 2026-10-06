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

/// <summary>
/// UI 스레드가 소유하는 설정·명령·화면 상태. 캡처는 AudioSession에 위임한다.
/// 타이머는 최신 숫자 snapshot만 읽으므로 입력 콜백마다 UI 작업이 누적되지 않는다.
/// </summary>
public sealed class MainViewModel : ObservableObject, IAsyncDisposable
{
    private readonly bool persistSettings;
    private readonly AudioSession session = new();
    private readonly AudioLevelProcessor processor = new();
    private readonly CharacterAnimator animator = new();
    private readonly SettingsStore store = new();
    // 벽시계 변경에 영향을 받지 않는 시간축. envelope에는 경과 시간, blink에는 누적 시간을 전달한다.
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly DispatcherTimer timer;
    private AudioSettings settings;
    private AudioDevice? selected;
    private bool busy, demo, closing, topmost, green, muted, characterBusy;
    private string status = "마이크를 선택하고 Start를 누르세요.";
    private string characterStatus = "3열: 닫힘·중간·열림 / 2행: 일반·눈감음. 투명 PNG의 중앙·바닥을 자동 정렬합니다.";
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

    /// <summary>
    /// QA는 persistSettings=false로 실제 사용자 설정을 읽거나 덮어쓰지 않는다.
    /// 명령 활성 조건은 마이크 수명과 별도 PNG 로딩 상태를 기준으로 UI에서 평가한다.
    /// </summary>
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
        DefaultCharacterCommand = new(() => { Character.RestoreDefault(); CharacterStatus = "기본 캐릭터로 복원했습니다."; }, () => !closing && !characterBusy);
        ConversationCommand = new(() => ApplyPreset(ReactionPreset.Conversation));
        SoftVoiceCommand = new(() => ApplyPreset(ReactionPreset.SoftVoice));
        SnappyCommand = new(() => ApplyPreset(ReactionPreset.Snappy));
        // 60Hz는 목표 갱신 주기다. 지연된 프레임도 Tick의 실제 dt로 계산해 반응 시간이 늘어나지 않게 한다.
        timer = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(1000.0 / 60) };
        timer.Tick += Tick; timer.Start();
    }
    public AudioDevice? SelectedDevice { get => selected; set { if (Set(ref selected, value)) RefreshCommands(); } }
    // 실행 자원이 남아 있거나 데모/수명 작업 중이면 장치를 바꾸거나 두 번째 입력을 시작하지 않는다.
    public bool CanChooseDevice => !busy && !closing && !session.HasResources && !demo;
    public string Status { get => status; private set => Set(ref status, value); }
    public string CharacterStatus { get => characterStatus; private set => Set(ref characterStatus, value); }
    public string Metrics { get => metrics; private set => Set(ref metrics, value); }
    public double VoiceLevel { get => level; private set => Set(ref level, value); }
    public double RawLevel { get => raw; private set => Set(ref raw, value); }
    /// <summary>
    /// 캐릭터 입 반응만 0으로 초기화한다. PCM 캡처와 RAW/RMS/Peak, blink/idle은 계속된다.
    /// </summary>
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
    // 정규화 분모가 0이 되지 않도록 상대 경계를 최소 1dB 간격으로 함께 조정한다.
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
    // 관련 속성과 명령의 CanExecuteChanged를 함께 갱신해 버튼 상태가 실제 수명을 반영하게 한다.
    private void RefreshCommands()
    {
        Notify(nameof(CanChooseDevice)); Notify(nameof(DemoLabel));
        StartCommand.Refresh(); StopCommand.Refresh(); RefreshCommand.Refresh(); DemoCommand.Refresh();
        ImportCharacterCommand.Refresh(); DefaultCharacterCommand.Refresh();
    }
    public async Task InitializeAsync() => await RefreshDevicesAsync();
    /// <summary>
    /// 장치 열거는 UI 밖에서 수행한다. await 후 UI에서 목록을 교체하고 이전 선택을 가능하면 유지한다.
    /// </summary>
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
    /// <summary>
    /// 세션 시작 전에 표시값을 초기화한다. 실제 입력 장치 생성과 Stop/Dispose 순서는 AudioSession이 담당한다.
    /// </summary>
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
    /// <summary>
    /// 데모를 끝내거나 마이크 종료를 기다린 뒤 화면을 무음으로 돌리고 세션 오류 안내를 보존한다.
    /// </summary>
    private async Task StopAsync()
    {
        busy = true; demo = false; RefreshCommands(); Status = "마이크 종료 중…";
        try { await session.StopAsync(); ResetLevels(); Status = session.Error ?? "중지됨 · 마이크 캡처가 해제되었습니다."; }
        finally { busy = false; RefreshCommands(); }
    }
    // 장치와 분리된 합성 숫자 데모를 시작한다. 종료는 StopCommand가 담당한다.
    private void ToggleDemo()
    {
        demo = true; ResetLevels(); RefreshCommands(); Status = "합성 데모 · 마이크를 사용하지 않습니다. Stop으로 종료하세요.";
    }
    /// <summary>
    /// 디코딩·정렬을 작업 스레드에서 마친 뒤 UI에 한 번만 적용한다.
    /// 닫기 시작 후 도착한 결과는 버리고 실패 안내는 마이크 Status와 별개로 표시한다.
    /// </summary>
    public async Task<bool> ImportCharacterAsync(string path)
    {
        if (closing || characterBusy) return false;
        characterBusy = true; CharacterStatus = "캐릭터 시트를 읽는 중…"; RefreshCommands();
        try
        {
            var sheet = await Task.Run(() => CharacterSheetLoader.Load(path));
            if (closing) return false;
            Character.UseSheet(sheet, Path.GetFileNameWithoutExtension(path));
            CharacterStatus = "캐릭터 적용 완료 · 미리보기와 방송창에 함께 적용됩니다.";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or FormatException or OverflowException or COMException)
        {
            if (!closing) CharacterStatus = "시트를 불러오지 못했습니다. 16MB 이하 RGBA PNG와 3열×2행 크기를 확인하세요. 기존 캐릭터를 유지합니다.";
            return false;
        }
        finally { characterBusy = false; RefreshCommands(); }
    }
    // 프리셋으로 불변 설정을 통째로 바꾼 뒤 모든 관련 바인딩을 다시 알린다.
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
    // Stop/Start/종료에서 지난 세션 레벨과 시간 간격을 버린다. blink는 독립 시계를 계속 사용한다.
    private void ResetLevels()
    {
        processor.Reset(); VoiceLevel = 0; RawLevel = 0; Metrics = "RMS 0.0000   Peak 0.0000   -120.0 dBFS";
        previousTime = clock.Elapsed.TotalSeconds;
        Character.Update(animator.Update(0, previousTime));
    }
    /// <summary>
    /// DispatcherTimer의 UI 갱신. 입력 종료를 감지하면 중지하고, 나머지는 최신 측정값 한 개로 계산한다.
    /// async void는 이벤트 핸들러에 한정하며 busy가 다음 Tick의 중복 Stop을 막는다.
    /// </summary>
    private async void Tick(object? sender, EventArgs e)
    {
        double now = clock.Elapsed.TotalSeconds;
        double dt = Math.Max(0, now - previousTime); previousTime = now;
        if (session.HasInputEnded && !busy && !closing) { await StopAsync(); return; }
        bool running = session.State == CaptureState.Running;
        var measured = session.ReadLevel(Stopwatch.GetTimestamp());
        // 데모에서는 발화/침묵 구간을 흉내 낸 dB 값을 만든다. 마이크를 열거나 소리를 재생하지 않는다.
        if (demo)
        {
            double db = Math.Sin(now * 2.8) > -.25 ? -35 + 22 * Math.Max(0, Math.Sin(now * 6)) : -120;
            double rms = db <= -120 ? 0 : Math.Pow(10, db / 20);
            measured = new(rms, rms, db);
        }
        var result = processor.Update(measured.Dbfs, dt, settings);
        // 입 음소거 또는 정지 중에는 envelope만 지운다. 음소거 중에도 입력 측정과 RAW는 표시한다.
        if ((!running && !demo) || CharacterMuted) processor.Reset();
        VoiceLevel = processor.VoiceLevel; RawLevel = running || demo ? result.Raw : 0;
        Metrics = $"RMS {measured.Rms:F4}   Peak {measured.Peak:F4}   {measured.Dbfs:F1} dBFS{(measured.Peak >= 1 ? " · CLIP" : "")}";
        Character.Update(animator.Update(VoiceLevel, now));
    }
    /// <summary>
    /// UI 타이머를 먼저 중단하고 캡처 해제 후 설정을 작업 스레드에서 저장한다.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        closing = true; timer.Stop(); timer.Tick -= Tick; RefreshCommands();
        // 진행 중인 Start/Stop 뒤에서 종료를 기다린다. UI에서 Wait/Result로 동기 대기하지 않는다.
        await session.DisposeAsync(); ResetLevels();
        bool saved = !persistSettings || await Task.Run(() => store.Save(new(settings, Topmost, GreenBackground)));
        if (!saved) Status = "설정을 저장하지 못했습니다. 다음 실행에서 기본값을 사용합니다.";
    }
}
