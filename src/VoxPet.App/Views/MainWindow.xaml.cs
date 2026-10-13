using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.IO;
using System.Text.Json;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;

namespace VoxPet.App.Views;

/// <summary>
/// 설정창의 표시·파일 대화상자·반응형 배치·방송창 수명만 담당하는 code-behind. 오디오 계산은 포함하지 않는다.
/// </summary>
public partial class MainWindow : Window
{
    public MainViewModel Model { get; }
    private CharacterWindow? character;
    private ExpressionHotkeys? hotkeys;
    private MuteHotkey? muteHotkey;
    private HandMotionInputs? handInputs;
    private ImageRuntimeInputs? runtimeInputs;
    private TrayService? tray;
    internal bool IsInTray { get; private set; }
    internal bool TrayIconVisible => tray?.Visible == true;
    internal void ActivateTrayMenu(int index) => tray?.ActivateMenu(index);
    private bool allowClose, shuttingDown, initializationStarted, restoringPlacement;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    // Loaded 후 장치 목록 초기화가 끝나는 시점. Windows smoke가 UI 준비를 기다리는 용도다.
    public Task Ready => ready.Task;
    public MainWindow(bool smoke = false) : this(smoke, SystemParameters.WorkArea) { }
    // 작업 영역 주입은 작은 화면의 시작 bounds를 실제 모니터 변경 없이 시험하기 위한 경로다.
    internal MainWindow(bool smoke, Rect initialWorkArea, MainViewModel? model = null)
    {
        Model = model ?? new(persistSettings: !smoke);
        InitializeComponent(); DataContext = Model;
        Width = Model.MainPlacement?.Width ?? Math.Min(Width, Math.Max(MinWidth, initialWorkArea.Width - 32));
        Height = Model.MainPlacement?.Height ?? Math.Min(Height, Math.Max(MinHeight, initialWorkArea.Height - 32));
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = initialWorkArea.Left + (initialWorkArea.Width - Width) / 2;
        Top = initialWorkArea.Top + (initialWorkArea.Height - Height) / 2;
        SettingsTabs.SizeChanged += (_, _) => UpdateResponsiveLayout();
        SizeChanged += (_, _) => { UpdateResponsiveLayout(); RememberPlacement(); };
        LocationChanged += (_, _) => RememberPlacement();
        StateChanged += (_, _) => RememberPlacement();
        Model.OpenBroadcast += () => ShowCharacter();
        Model.MoveToTray += HideToTray;
        Model.OpenMicrophonePrivacy += () =>
        {
            try { Process.Start(new ProcessStartInfo("ms-settings:privacy-microphone") { UseShellExecute = true }); }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
            { MessageBox.Show(this, "Windows 설정 → 개인정보 및 보안 → 마이크를 직접 열어 접근 권한을 확인하세요.", "VoxPet"); }
        };
        Model.ChooseCharacterSheet += () =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Title = "캐릭터 PNG 시트 선택 (3열 또는8열×2행)", Filter = "PNG 시트 (*.png)|*.png", CheckFileExists = true, Multiselect = false };
            return dialog.ShowDialog(this) == true ? dialog.FileName : null;
        };
        Model.Runtime.ChooseManifest += () =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Title = "이미지 조합 세트 등록", Filter = "이미지 세트 (*.json)|*.json", CheckFileExists = true, Multiselect = false };
            return dialog.ShowDialog(this) == true ? dialog.FileName : null;
        };
        Model.Wardrobe.ChooseImage += () =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Title = "의상 이미지 추가", Filter = "이미지|*.png;*.jpg;*.jpeg;*.bmp;*.gif", CheckFileExists = true };
            return dialog.ShowDialog(this) == true ? dialog.FileName : null;
        };
        Model.Expressions.ChooseSheet += () =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Title = "슬롯 표정 PNG 시트 선택 (3열 또는8열×2행)", Filter = "PNG 시트 (*.png)|*.png", CheckFileExists = true, Multiselect = false };
            return dialog.ShowDialog(this) == true ? dialog.FileName : null;
        };
        PreviewKeyDown += (_, e) =>
        {
            if (Model.Runtime.Capture(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers)) { e.Handled = true; return; }
            if (Model.HandleMuteKey(e.Key, Keyboard.Modifiers, e.IsRepeat) || Model.HandleExpressionKey(e.Key, Keyboard.Modifiers, e.IsRepeat)) e.Handled = true;
        };
        SourceInitialized += (_, _) =>
        {
            RestorePlacement(false);
            if (!smoke) { EnableExpressionHotkeys(); EnableMuteHotkey(); EnableHandInputs(); EnableRuntimeInputs(); }
            else Model.Expressions.HotkeyStatus = "QA: 전역 단축키 별도 fixture에서 검증 · F12는 앱 안에서 사용";
        };
        Closed += (_, _) => { hotkeys?.Dispose(); muteHotkey?.Dispose(); handInputs?.Dispose(); runtimeInputs?.Dispose(); tray?.Dispose(); };
        Loaded += async (_, _) =>
        {
            if (initializationStarted) return; initializationStarted = true;
            try
            {
                RestorePlacement(true); RememberPlacement();
                await Model.InitializeAsync();
                if (!shuttingDown && Model.BroadcastOpen) ShowCharacter();
                RememberPlacement(); ready.TrySetResult();
                if (!smoke && AppPaths.QaFolder is { } folder)
                {
                    Directory.CreateDirectory(folder);
                    if (Environment.GetEnvironmentVariable("VOXPET_QA_START_IN_TRAY") == "true") HideToTray();
                    File.WriteAllText(Path.Combine(folder, "startup-ready.json"), JsonSerializer.Serialize(new
                    {
                        initialized = true,
                        familyCount = Model.Character.DefaultExpressionCount,
                        mouthFrames = Model.Character.MouthFrameCount,
                        characterId = Model.SelectedCharacter.Id,
                        characterName = Model.Character.Name,
                        remembered = Model.SnapshotSettings() with { MicrophoneId = null },
                        microphonePreferencePresent = Model.PreferredMicrophoneId != null,
                        selectedStorageId = Model.Expressions.Selected.Index,
                        selectedPosition = Model.Expressions.Selected.Position,
                        canEdit = Model.Expressions.CanEdit,
                        visible = IsVisible,
                        hwnd = new WindowInteropHelper(this).Handle.ToInt64()
                    }));
                    if (IsVisible) await CaptureStartupScreenAsync();
                }
            }
            catch (Exception error)
            {
                string path = ErrorDiagnostics.Record(error, "initialization");
                if (smoke) ready.TrySetException(error);
                else if (AppPaths.QaFolder != null) { ready.TrySetException(error); System.Windows.Application.Current.Shutdown(1); }
                else
                {
                    Model.ReportStartupIssue(); ready.TrySetResult();
                    MessageBox.Show(this, $"일부 시작 설정을 읽지 못했습니다. 창을 유지합니다. 오류 기록: {path}", "VoxPet 시작 설정");
                }
            }
        };
        Closing += OnClosing;
    }
    private async Task CaptureStartupScreenAsync()
    {
        if (AppPaths.QaFolder is not { } folder || Environment.GetEnvironmentVariable("VOXPET_QA_SCREEN_NAME") is not { } name) return;
        try { await ScreenEvidence.CaptureAsync(this, name, (passed, _) => { if (!passed) throw new InvalidOperationException("Startup desktop capture invalid"); }, new { normalStartup = true }); }
        catch (Exception error) { File.WriteAllText(Path.Combine(folder, "screen-error.json"), JsonSerializer.Serialize(new { type = error.GetType().Name })); }
    }
    private void RestorePlacement(bool restoreState)
    {
        if (Model.MainPlacement is not { } saved) return;
        restoringPlacement = true;
        try
        {
            WindowPlacementService.Restore(this, saved);
            Model.RememberMainPlacement(WindowPlacementService.Capture(this) with { Maximized = saved.Maximized });
            if (restoreState && saved.Maximized) WindowState = WindowState.Maximized;
        }
        finally { restoringPlacement = false; }
    }
    private void RememberPlacement()
    {
        if (!IsLoaded || restoringPlacement || shuttingDown) return;
        Model.RememberMainPlacement(WindowPlacementService.Capture(this, Model.MainPlacement));
    }
    internal ExpressionHotkeys EnableExpressionHotkeys()
    {
        if (hotkeys != null) return hotkeys;
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        hotkeys = new(source, slot => _ = Model.Expressions.ActivateAsync(slot));
        Model.IsGlobalExpressionKey = slot => hotkeys.Registered.Contains(slot);
        Model.Expressions.HotkeyStatus = hotkeys.Conflicts.Count == 0
            ? "Ctrl+Shift+F1~F11 전역 사용 · F12는 VoxPet 창 안에서 사용"
            : $"단축키 등록 실패: {string.Join(", ", hotkeys.Conflicts.Select(i => $"F{i + 1}"))} · 다른 앱과 충돌할 수 있습니다. 앱 안의 키/버튼을 사용하세요. F12도 앱 안에서 사용합니다.";
        return hotkeys;
    }
    internal HandMotionInputs EnableHandInputs()
    {
        if (handInputs != null) return handInputs;
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        handInputs = new(source, gesture => Model.Hands.Trigger(gesture), Model.Hands.LeftClick);
        Model.Hands.InputsChanged += ConfigureHandInputs; ConfigureHandInputs(); return handInputs;
    }
    private void EnableRuntimeInputs()
    {
        runtimeInputs = new(HwndSource.FromHwnd(new WindowInteropHelper(this).Handle), Model.Runtime.Execute);
        Model.Runtime.InputsChanged += ConfigureRuntimeInputs; ConfigureRuntimeInputs();
    }
    private void ConfigureRuntimeInputs()
    {
        if (runtimeInputs == null) return;
        var runtime = Model.Runtime; runtimeInputs.Configure(runtime.Bindings, runtime.LeftClick, runtime.CanUse && !runtime.Recording, runtime.SceneRevision);
        ConfigureHandInputs();
        if (runtime.CanUse && !runtime.Recording) runtime.ReportInputStatus(
            runtime.Bindings.Length > 0 && !runtimeInputs.KeyboardRegistered || runtime.LeftClick && !runtimeInputs.MouseRegistered
            ? "입력 감지 등록 실패 · 화면의 액션 재생 버튼을 사용하세요." : "전역 입력 준비 · 원래 키와 클릭은 게임에 전달합니다.");
    }
    private async void ExportImageTemplate(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "제작 틀을 저장할 폴더 선택" };
        if (dialog.ShowDialog(this) != true) return;
        string id = Model.SelectedCharacter.Id;
        string target = Path.Combine(dialog.FolderName, "VoxPet-" + id + "-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            await Task.Run(() =>
            {
                string source = Path.Combine(AppContext.BaseDirectory, "Assets", "ImageSetTemplate");
                var pack = ImageSetLoader.Load(Path.Combine(source, "imageset.json"));
                Directory.CreateDirectory(target);
                foreach (string file in pack.Definition.Files) File.Copy(Path.Combine(source, file), Path.Combine(target, file));
                File.WriteAllText(Path.Combine(target, "imageset.json"), JsonSerializer.Serialize(pack.Definition with { CharacterId = id }, VoxPet.Core.Models.ImageSet.JsonOptions));
            });
            MessageBox.Show(this, $"제작 틀을 저장했습니다. 예제 PNG를 캐릭터 그림으로 교체하세요.\n{target}", "VoxPet 제작 가이드");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or JsonException)
        { MessageBox.Show(this, "제작 틀 저장 실패: " + error.Message, "VoxPet 제작 가이드"); }
    }
    private void RecordRuntimeKey(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button { Tag: VoxPet.Core.Models.RuntimeOperation operation }) Model.Runtime.Record(operation);
    }
    private void ConfigureHandInputs()
    {
        if (handInputs == null) return;
        var settings = Model.Hands.Snapshot();
        if (Model.Runtime.IsActive) settings = settings with { Profiles = settings.Profiles.Select(p => p with { Enabled = false }).ToArray() };
        handInputs.Configure(settings);
        bool needsMouse = settings.Profiles.Any(profile => profile.Enabled && profile.LeftClick && profile.Mode == VoxPet.Core.Models.HandTriggerMode.Hotkey);
        Model.Hands.Status = handInputs.Conflicts.Count > 0 || needsMouse && !handInputs.MouseRegistered
            ? "입력 등록 실패 · 다른 앱과의 키 충돌을 확인하세요. 손동작 시험 버튼은 사용할 수 있습니다."
            : "동작 설정 자동 저장 · 좌클릭은 원래 클릭과 함께 실행됩니다.";
    }
    internal MuteHotkey EnableMuteHotkey()
    {
        if (muteHotkey != null) return muteHotkey;
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        muteHotkey = new(source, () => Model.CharacterMuted = !Model.CharacterMuted);
        Model.IsGlobalMuteKey = () => muteHotkey.Registered;
        Model.MuteHotkeyStatus = muteHotkey.Registered
            ? "Ctrl+Shift+M · 다른 앱에서도 캐릭터 입 음소거 전환"
            : "Ctrl+Shift+M 등록 실패 · 다른 앱과 충돌할 수 있습니다. VoxPet 창 안의 키 또는 체크박스를 사용하세요.";
        return muteHotkey;
    }
    internal void HideToTray()
    {
        if (shuttingDown) return;
        tray ??= new TrayService(RestoreFromTray, () => Model.ObsBroadcastCommand.Execute(null), Close);
        tray.Show(); IsInTray = true; Hide();
    }
    internal void RestoreFromTray()
    {
        if (shuttingDown) return;
        Show(); WindowState = Model.MainPlacement?.Maximized == true ? WindowState.Maximized : WindowState.Normal; Activate(); IsInTray = false; tray?.Hide();
        if (AppPaths.QaFolder is { } folder)
        {
            File.WriteAllText(Path.Combine(folder, "activated.json"), JsonSerializer.Serialize(new { visible = IsVisible, hwnd = new WindowInteropHelper(this).Handle.ToInt64() }));
            _ = CaptureStartupScreenAsync();
        }
    }
    /// <summary>작은 창은 공통 미리보기를 숨기고 탭을 넓힌다. 캐릭터 탭에서는 항상 미리보기를 볼 수 있다.</summary>
    private void UpdateResponsiveLayout()
    {
        bool compact = ActualWidth < 870 || ActualHeight < 600;
        LayoutRoot.Margin = new Thickness(compact ? 10 : 18);
        NavigationRail.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        NavigationColumn.Width = new GridLength(compact ? 0 : 148);
        if (compact) SettingsTabs.ClearValue(System.Windows.Controls.Control.TemplateProperty);
        else SettingsTabs.Template = (System.Windows.Controls.ControlTemplate)FindResource("ContentOnlyTabs");
        ProjectHeading.Visibility = ActualWidth < 640 ? Visibility.Collapsed : Visibility.Visible;
        HeaderTray.Visibility = ActualWidth < 640 ? Visibility.Collapsed : Visibility.Visible;
        TitleText.FontSize = compact ? 22 : 26;
        SubtitleText.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        StatusCard.Padding = new Thickness(compact ? 8 : 14);
        StatusCard.Margin = new Thickness(0, compact ? 8 : 16, 0, 0);
        PreviewCard.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        PanelsGrid.ColumnDefinitions[1].Width = new GridLength(compact ? 0 : 16);
        PanelsGrid.ColumnDefinitions[2].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        System.Windows.Controls.Grid.SetColumn(SettingsTabs, compact ? 0 : 2);
        PreviewCard.Height = Math.Max(280, SettingsTabs.ActualHeight);
    }
    /// <summary>
    /// 하나의 방송창을 생성·복구·활성화한다. 닫힌 창은 다음 호출에서 새로 만든다.
    /// </summary>
    public CharacterWindow ShowCharacter()
    {
        if (character == null)
        {
            // Owner를 지정하지 않아 설정창 최소화가 방송창을 함께 숨기지 않게 한다. DataContext만 공유한다.
            character = new CharacterWindow() { DataContext = Model };
            character.Closed += (_, _) => { if (!shuttingDown) Model.RememberBroadcastOpen(false); character = null; };
            Model.RememberBroadcastOpen(true); character.Show();
        }
        else
        {
            if (character.WindowState == WindowState.Minimized) character.WindowState = WindowState.Normal;
            character.Activate();
        }
        return character;
    }
    /// <summary>
    /// 첫 Closing을 취소하고 UI를 잠근 뒤 캡처 종료를 await한다. 실패하면 창을 유지해 재시도를 허용한다.
    /// </summary>
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (allowClose) return;
        e.Cancel = true;
        if (shuttingDown) return;
        RememberPlacement(); shuttingDown = true; IsEnabled = false;
        try
        {
            await ShutdownAsync();
            // 종료가 동기적으로 끝나도 취소한 Closing 이벤트가 반환된 뒤 다시 Close하도록 Dispatcher에 예약한다.
            _ = Dispatcher.BeginInvoke(Close);
        }
        catch { IsEnabled = true; shuttingDown = false; if (!IsVisible) RestoreFromTray(); MessageBox.Show("마이크 종료에 실패했습니다. 잠시 후 다시 닫아주세요.", "VoxPet"); }
    }
    /// <summary>
    /// 캡처 종료·설정 저장 후 방송창을 닫고 다음 Closing을 허용한다.
    /// </summary>
    public async Task ShutdownAsync() { runtimeInputs?.Dispose(); Model.Runtime.InputsChanged -= ConfigureRuntimeInputs; handInputs?.Dispose(); Model.Hands.InputsChanged -= ConfigureHandInputs; await Model.DisposeAsync(); hotkeys?.Dispose(); muteHotkey?.Dispose(); character?.Close(); tray?.Dispose(); allowClose = true; }
}
