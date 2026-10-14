using System.Windows;
using System.Windows.Controls;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;

namespace VoxPet.App.Views;

public partial class StudioShell : UserControl
{
    public StudioShell() { InitializeComponent(); }
    private StudioShellViewModel? Model => DataContext as StudioShellViewModel;
    private void QuickLegacy(object sender, RoutedEventArgs e)
    { if (Model != null && sender is FrameworkElement { Tag: string tag } && int.TryParse(tag, out int i)) Model.Main.Hands.Trigger((HandGesture)i, true); }
    private void WearNative(object sender, RoutedEventArgs e)
    { if (Model?.Main.Packages.Scene is { } scene && sender is FrameworkElement { Tag: string id }) scene.Runtime.Attachments.SetEnabled(id, !scene.Runtime.Attachments.EnabledIds.Contains(id)); }
    private void NativeExpression(object sender, RoutedEventArgs e)
    { if (Model != null && sender is FrameworkElement { Tag: string id }) Model.Main.Packages.Expression(id); }
    private void LegacySettings(object sender, RoutedEventArgs e)
    { if (Window.GetWindow(this) is MainWindow window && sender is FrameworkElement { Tag: string tab } && int.TryParse(tab, out int i)) window.ShowLegacySettings(i); }
    private void NativeAction(object sender, RoutedEventArgs e)
    { if (Model != null && sender is FrameworkElement { Tag: string id }) Model.Main.Packages.Play(id); }
}
