using System.ComponentModel;
using System.Windows;
using VoxPet.App.ViewModels;

namespace VoxPet.App.Views;

public partial class MainWindow : Window
{
    public MainViewModel Model { get; }
    private CharacterWindow? character;
    private bool allowClose, shuttingDown;
    private readonly TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Ready => ready.Task;
    public MainWindow(bool smoke = false) : this(smoke, SystemParameters.WorkArea) { }
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
        Loaded += async (_, _) => { await Model.InitializeAsync(); ready.TrySetResult(); };
        Closing += OnClosing;
    }
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
    public CharacterWindow ShowCharacter()
    {
        if (character == null)
        {
            // Keep broadcast output visible when the settings window is minimized.
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
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (allowClose) return;
        e.Cancel = true;
        if (shuttingDown) return;
        shuttingDown = true; IsEnabled = false;
        try
        {
            await ShutdownAsync();
            // Closing may finish synchronously; enqueue Close after the canceled event returns.
            _ = Dispatcher.BeginInvoke(() => { if (IsVisible) Close(); });
        }
        catch { IsEnabled = true; shuttingDown = false; MessageBox.Show("마이크 종료에 실패했습니다. 잠시 후 다시 닫아주세요.", "VoxPet"); }
    }
    public async Task ShutdownAsync() { await Model.DisposeAsync(); character?.Close(); allowClose = true; }
}
