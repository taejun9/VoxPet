using System.Windows;
using System.Windows.Input;

namespace VoxPet.App.Views;

/// <summary>
/// 공유 ViewModel을 표시하는 방송창. 입력 장치를 직접 열지 않으며 닫아도 마이크 세션은 계속된다.
/// </summary>
public partial class CharacterWindow : Window
{
    public CharacterWindow()
    {
        InitializeComponent();
        SizeChanged += (_, e) =>
        {
            if (IsLoaded && DataContext is VoxPet.App.ViewModels.MainViewModel model)
            { model.BroadcastWidth = e.NewSize.Width; model.BroadcastHeight = e.NewSize.Height; }
        };
        PreviewKeyDown += (_, e) =>
        {
            if (DataContext is VoxPet.App.ViewModels.MainViewModel model &&
                (model.HandleMuteKey(e.Key, Keyboard.Modifiers, e.IsRepeat) || model.HandleExpressionKey(e.Key, Keyboard.Modifiers, e.IsRepeat))) e.Handled = true;
        };
        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
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
