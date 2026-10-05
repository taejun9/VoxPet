using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Input;

namespace VoxPet.App.Views;

/// <summary>Windows smoke fixture; exercises real layout/scrolling without a microphone.</summary>
internal static class LayoutQa
{
    private static IEnumerable<FrameworkElement> Descendants(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement element) yield return element;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static bool FullyVisible(FrameworkElement element, FrameworkElement root)
    {
        if (!element.IsVisible || element.ActualWidth <= 0 || element.ActualHeight <= 0) return false;
        for (DependencyObject? ancestor = element; ancestor != null; ancestor = VisualTreeHelper.GetParent(ancestor))
        {
            if (ancestor is FrameworkElement viewport && (ReferenceEquals(viewport, root) || viewport is ScrollContentPresenter))
            {
                var bounds = element.TransformToAncestor(viewport).TransformBounds(new Rect(element.RenderSize));
                if (bounds.Left < -.5 || bounds.Top < -.5 || bounds.Right > viewport.ActualWidth + .5 || bounds.Bottom > viewport.ActualHeight + .5) return false;
            }
            if (ReferenceEquals(ancestor, root)) return true;
        }
        return false;
    }

    public static async Task RunAsync(MainWindow main, Action<bool, string> check)
    {
        var cases = new List<object>();
        var startupCases = new List<object>();
        foreach (var area in new[] { new Rect(10, 20, 960, 540), new Rect(10, 20, 640, 480), new Rect(10, 20, 480, 320) })
        {
            var startup = new MainWindow(smoke: true, initialWorkArea: area); startup.Show(); await startup.Ready;
            bool inside = startup.Left >= area.Left && startup.Top >= area.Top && startup.Left + startup.ActualWidth <= area.Right + 1 && startup.Top + startup.ActualHeight <= area.Bottom + 1;
            check(inside, $"layout_initial_work_area_{area.Width}x{area.Height}");
            startupCases.Add(new { simulatedWorkArea = true, workArea = new { area.Left, area.Top, area.Width, area.Height }, startup.Left, startup.Top, startup.ActualWidth, startup.ActualHeight, inside });
            startup.Close(); await Task.Delay(100);
            check(!startup.IsVisible, "layout_fixture_closed");
        }
        var root = (FrameworkElement)main.Content;
        foreach (var (width, height) in new[] { (1000, 730), (960, 540), (640, 480), (480, 320) })
        {
            main.Width = width; main.Height = height;
            await Task.Delay(100); main.UpdateLayout();
            check(Math.Abs(main.ActualWidth - width) < 1 && Math.Abs(main.ActualHeight - height) < 1, $"layout_requested_size_{width}x{height}");
            var expander = Descendants(root).OfType<Expander>().Single();
            expander.IsExpanded = true; main.UpdateLayout();
            var controls = Descendants(root).Where(element => element is Slider or CheckBox or ComboBox || element is Button { Command: not null }).ToArray();
            var invisible = new List<string>();
            foreach (var control in controls)
            {
                control.BringIntoView(); await Task.Delay(50); main.UpdateLayout();
                string label = control is ContentControl content ? content.Content?.ToString() ?? control.GetType().Name : System.Windows.Automation.AutomationProperties.GetName(control);
                bool visible = FullyVisible(control, root);
                check(visible, $"layout_control_{width}x{height}_{label}");
                if (!visible) invisible.Add(label);
            }
            expander.IsExpanded = false;
            foreach (var scroll in Descendants(root).OfType<ScrollViewer>()) scroll.ScrollToTop();
            await Task.Delay(50); main.UpdateLayout();
            if (width >= 870 && height >= 600)
                check(FullyVisible(main.PreviewCard, root), $"layout_preview_card_at_top_{width}x{height}");
            bool? wheelScroll = null;
            if (width < 870 || height < 600)
            {
                check(controls.OfType<Button>().Where(button => Equals(button.Content, "Start") || Equals(button.Content, "Stop")).All(button => FullyVisible(button, root)), $"layout_start_stop_at_top_{width}x{height}");
                double before = main.ContentViewport.VerticalOffset;
                var start = controls.OfType<Button>().Single(button => Equals(button.Content, "Start"));
                start.RaiseEvent(new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, -120) { RoutedEvent = Mouse.MouseWheelEvent });
                await Task.Delay(50); main.UpdateLayout();
                wheelScroll = main.ContentViewport.VerticalOffset > before;
                check(wheelScroll.Value, $"layout_wheel_scroll_{width}x{height}");
                main.ContentViewport.ScrollToTop(); await Task.Delay(50); main.UpdateLayout();
            }
            foreach (double scale in new[] { 1.0, 1.5, 2.0 })
            {
                var bitmap = new RenderTargetBitmap((int)Math.Ceiling(main.ActualWidth * scale), (int)Math.Ceiling(main.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
                bitmap.Render(main);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var file = File.Create(Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), $"voxpet-layout-{width}x{height}-{scale:F1}.png"));
                encoder.Save(file);
            }
            cases.Add(new { requestedWidth = width, requestedHeight = height, actualWidth = main.ActualWidth, actualHeight = main.ActualHeight, controls = controls.Length, invisible, wheelScroll });
        }
        main.Width = 1000; main.Height = 730; await Task.Delay(100); main.UpdateLayout();
        File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), "voxpet-layout-result.json"), JsonSerializer.Serialize(new { syntheticRenderScale = true, physicalDpiChange = false, startupCases, cases }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
