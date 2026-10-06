using System.ComponentModel;
using System.Windows;
using VoxPet.App.ViewModels;

namespace VoxPet.App.Views;

/// <summary>
/// 설정창의 표시·파일 대화상자·반응형 배치·방송창 수명만 담당하는 code-behind. 오디오 계산은 포함하지 않는다.
/// </summary>
public partial class MainWindow : Window
{
    public MainViewModel Model { get; }
    private CharacterWindow? character;
    private bool allowClose, shuttingDown;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    // Loaded 후 장치 목록 초기화가 끝나는 시점. Windows smoke가 UI 준비를 기다리는 용도다.
    public Task Ready => ready.Task;
    public MainWindow(bool smoke = false) : this(smoke, SystemParameters.WorkArea) { }
    // 작업 영역 주입은 작은 화면의 시작 bounds를 실제 모니터 변경 없이 시험하기 위한 경로다.
    internal MainWindow(bool smoke, Rect initialWorkArea)
    {
        Model = new(persistSettings: !smoke);
        InitializeComponent(); DataContext = Model;
        Width = Math.Min(Width, Math.Max(MinWidth, initialWorkArea.Width - 32));
        Height = Math.Min(Height, Math.Max(MinHeight, initialWorkArea.Height - 32));
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = initialWorkArea.Left + (initialWorkArea.Width - Width) / 2;
        Top = initialWorkArea.Top + (initialWorkArea.Height - Height) / 2;
        ContentViewport.SizeChanged += (_, _) => UpdateResponsiveLayout();
        SizeChanged += (_, _) => UpdateResponsiveLayout();
        Model.OpenBroadcast += () => ShowCharacter();
        Model.ChooseCharacterSheet += () =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Title = "캐릭터 PNG 시트 선택 (3열×2행)", Filter = "PNG 시트 (*.png)|*.png", CheckFileExists = true, Multiselect = false };
            return dialog.ShowDialog(this) == true ? dialog.FileName : null;
        };
        Loaded += async (_, _) => { await Model.InitializeAsync(); ready.TrySetResult(); };
        Closing += OnClosing;
    }
    /// <summary>
    /// 870 DIP 미만의 폭 또는 600 DIP 미만의 높이에서는 조작 영역을 먼저 보여 주는 한 열 배치로 전환한다.
    /// 하나의 ScrollViewer가 전체 내용을 스크롤하므로 작은 창에서도 모든 조작에 접근할 수 있다.
    /// </summary>
    private void UpdateResponsiveLayout()
    {
        bool compact = ActualWidth < 870 || ActualHeight < 600;
        LayoutRoot.Margin = new Thickness(compact ? 16 : 28);
        TitleText.FontSize = compact ? 26 : 32;
        SubtitleText.Margin = new Thickness(0, compact ? 2 : 6, 0, compact ? 8 : 24);
        StatusCard.Padding = new Thickness(compact ? 10 : 14);
        StatusCard.Margin = new Thickness(0, compact ? 12 : 20, 0, 0);
        StatusCard.MaxHeight = compact ? 52 : 80;
        PanelsGrid.RowDefinitions[1].Height = compact ? GridLength.Auto : new GridLength(0);
        PanelsGrid.ColumnDefinitions[1].Width = new GridLength(compact ? 0 : 24);
        PanelsGrid.ColumnDefinitions[2].Width = compact ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
        System.Windows.Controls.Grid.SetColumn(ControlsPanel, compact ? 0 : 2);
        System.Windows.Controls.Grid.SetRow(PreviewCard, compact ? 1 : 0);
        PreviewCard.Height = compact ? 400 : Math.Max(280, ContentViewport.ActualHeight);
        PreviewCard.Margin = new Thickness(0, compact ? 20 : 0, 0, 0);
    }
    /// <summary>
    /// 하나의 방송창을 생성·복구·활성화한다. 닫힌 창은 다음 호출에서 새로 만든다.
    /// </summary>
    public CharacterWindow ShowCharacter()
    {
        if (character == null)
        {
            // Owner를 지정하지 않아 설정창 최소화가 방송창을 함께 숨기지 않게 한다. DataContext만 공유한다.
            character = new CharacterWindow { DataContext = Model };
            character.Closed += (_, _) => character = null;
            character.Show();
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
        shuttingDown = true; IsEnabled = false;
        try
        {
            await ShutdownAsync();
            // 종료가 동기적으로 끝나도 취소한 Closing 이벤트가 반환된 뒤 다시 Close하도록 Dispatcher에 예약한다.
            _ = Dispatcher.BeginInvoke(() => { if (IsVisible) Close(); });
        }
        catch { IsEnabled = true; shuttingDown = false; MessageBox.Show("마이크 종료에 실패했습니다. 잠시 후 다시 닫아주세요.", "VoxPet"); }
    }
    /// <summary>
    /// 캡처 종료·설정 저장 후 방송창을 닫고 다음 Closing을 허용한다.
    /// </summary>
    public async Task ShutdownAsync() { await Model.DisposeAsync(); character?.Close(); allowClose = true; }
}
