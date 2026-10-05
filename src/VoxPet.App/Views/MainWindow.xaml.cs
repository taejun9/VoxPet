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
    public MainWindow(bool smoke = false)
    {
        Model = new(persistSettings: !smoke);
        InitializeComponent(); DataContext = Model;
        Model.OpenBroadcast += () => ShowCharacter();
        Loaded += async (_, _) => { await Model.InitializeAsync(); ready.TrySetResult(); };
        Closing += OnClosing;
    }
    public CharacterWindow ShowCharacter()
    {
        if (character == null)
        {
            character = new CharacterWindow { DataContext = Model, Owner = this };
            character.Closed += (_, _) => character = null;
            character.Show();
        }
        else character.Activate();
        return character;
    }
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (allowClose) return;
        e.Cancel = true;
        if (shuttingDown) return;
        shuttingDown = true; IsEnabled = false;
        try { await ShutdownAsync(); allowClose = true; Close(); }
        catch { IsEnabled = true; shuttingDown = false; MessageBox.Show("마이크 종료에 실패했습니다. 잠시 후 다시 닫아주세요.", "VoxPet"); }
    }
    public async Task ShutdownAsync() { await Model.DisposeAsync(); character?.Close(); }
}
