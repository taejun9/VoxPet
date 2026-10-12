using System.Windows;
using System.Windows.Input;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;

namespace VoxPet.App.Views;

/// <summary>
/// 공유 ViewModel을 표시하는 방송창. 입력 장치를 직접 열지 않으며 닫아도 마이크 세션은 계속된다.
/// </summary>
public partial class CharacterWindow : Window
{
    private bool restoringPlacement;
    public CharacterWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => RestorePlacement();
        Loaded += (_, _) => { RestorePlacement(); RememberPlacement(); };
        SizeChanged += (_, _) => RememberPlacement();
        LocationChanged += (_, _) => RememberPlacement();
        StateChanged += (_, _) => RememberPlacement();
        PreviewKeyDown += (_, e) =>
        {
            if (DataContext is VoxPet.App.ViewModels.MainViewModel model &&
                (model.HandleMuteKey(e.Key, Keyboard.Modifiers, e.IsRepeat) || model.HandleExpressionKey(e.Key, Keyboard.Modifiers, e.IsRepeat))) e.Handled = true;
        };
        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
    }
    private void RestorePlacement()
    {
        if (DataContext is not MainViewModel model || model.BroadcastPlacement is not { } saved) return;
        restoringPlacement = true;
        try { WindowPlacementService.Restore(this, saved with { Width = model.BroadcastWidth, Height = model.BroadcastHeight }, true); }
        finally { restoringPlacement = false; }
    }
    private void RememberPlacement()
    {
        if (!IsLoaded || restoringPlacement || DataContext is not MainViewModel model) return;
        model.RememberBroadcastPlacement(WindowPlacementService.Capture(this, model.BroadcastPlacement));
    }
    // 테두리 없는 창이므로 캐릭터/배경의 좌클릭 드래그를 WPF 창 이동으로 연결한다.
    private void DragCharacter(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }
    private void ResizeCharacter(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem { Tag: string size } && double.TryParse(size, out double value) && DataContext is VoxPet.App.ViewModels.MainViewModel model)
        { model.BroadcastWidth = value; model.BroadcastHeight = value; }
    }
    private void CloseCharacter(object sender, RoutedEventArgs e) => Close();
}
