using System.Windows;
using System.Windows.Input;

namespace VoxPet.App.Views;

public partial class CharacterWindow : Window
{
    public CharacterWindow()
    {
        InitializeComponent();
        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
    }
    private void DragCharacter(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }
    private void CloseCharacter(object sender, RoutedEventArgs e) => Close();
}
