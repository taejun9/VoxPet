using System.Windows;
using System.Windows.Controls;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;

namespace VoxPet.App.Views;

public partial class StudioShell : UserControl
{
    private StudioShellViewModel? attached;
    public StudioShell()
    {
        InitializeComponent(); SizeChanged += (_, _) => Responsive(); Loaded += (_, _) => Attach(); Unloaded += (_, _) => Detach(); DataContextChanged += (_, _) => Attach();
    }
    private void Attach() { Detach(); attached = Model; if (attached != null) attached.PropertyChanged += Changed; Responsive(); }
    private void Detach() { if (attached != null) attached.PropertyChanged -= Changed; attached = null; }
    private void Changed(object? sender, System.ComponentModel.PropertyChangedEventArgs e) { if (e.PropertyName == nameof(StudioShellViewModel.Page)) { Responsive(); PageScroll.ScrollToTop(); } }
    private void Responsive()
    {
        if (SidebarColumn == null) return;
        SidebarColumn.Width = new GridLength(ActualWidth < 640 ? 95 : 135);
        bool hasPreview = Model?.Page is not (1 or 5);
        bool compact = ActualWidth < 870;
        PreviewColumn.Width = new GridLength(hasPreview && !compact ? 290 : 0);
        SharedPreview.Visibility = hasPreview ? Visibility.Visible : Visibility.Collapsed;
        Grid.SetColumn(SharedPreview, compact ? 0 : 1); Grid.SetRow(SharedPreview, compact ? 1 : 0);
    }
    private StudioShellViewModel? Model => DataContext as StudioShellViewModel;
    private void QuickLegacy(object sender, RoutedEventArgs e)
    { if (Model != null && sender is FrameworkElement { Tag: string tag } && int.TryParse(tag, out int i)) Model.Main.Hands.Trigger((HandGesture)i, true); }
    private void WearNative(object sender, RoutedEventArgs e)
    { if (Model?.Main.Packages.Scene is { } scene && sender is FrameworkElement { Tag: string id }) scene.Runtime.Attachments.SetEnabled(id, !scene.Runtime.Attachments.EnabledIds.Contains(id)); }
    private void NativeExpression(object sender, RoutedEventArgs e)
    { if (Model != null && sender is FrameworkElement { Tag: string id }) Model.Main.Packages.Expression(id); }
    private void LegacySettings(object sender, RoutedEventArgs e)
    { if (Window.GetWindow(this) is MainWindow window && sender is FrameworkElement { Tag: string tab } && int.TryParse(tab, out int i)) window.ShowStudioSettings(i); }
    private void NativeAction(object sender, RoutedEventArgs e)
    { if (Model != null && sender is FrameworkElement { Tag: string id }) Model.Main.Packages.Play(id); }
}
