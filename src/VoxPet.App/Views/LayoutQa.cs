using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.Services;

namespace VoxPet.App.Views;

/// <summary>마이크 없이 실제 Windows WPF 탭 배치와 선택 상자 색상을 시험하는 smoke fixture. 물리 DPI 변경 시험과 구분한다.</summary>
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

    /// <summary>
    /// 자신의 IsVisible뿐 아니라 조상 viewport에 의해 잘리는지 확인한다. 소수점 배치 오차는 0.5 DIP까지 허용한다.
    /// </summary>
    private static bool FullyVisible(FrameworkElement element, FrameworkElement root)
    {
        if (!element.IsVisible || element.ActualWidth <= 0 || element.ActualHeight <= 0) return false;
        for (DependencyObject? ancestor = element; ancestor != null; ancestor = VisualTreeHelper.GetParent(ancestor))
        {
            if (ancestor is FrameworkElement viewport && (ReferenceEquals(viewport, root) || viewport is ScrollContentPresenter or Viewbox or TabControl))
            {
                var bounds = element.TransformToAncestor(viewport).TransformBounds(new Rect(element.RenderSize));
                if (bounds.Left < -.5 || bounds.Top < -.5 || bounds.Right > viewport.ActualWidth + .5 || bounds.Bottom > viewport.ActualHeight + .5) return false;
            }
            if (ReferenceEquals(ancestor, root)) return true;
        }
        return false;
    }

    private static void Save(Window main, string name, double scale)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(main.ActualWidth * scale), (int)Math.Ceiling(main.ActualHeight * scale), 96 * scale, 96 * scale, PixelFormats.Pbgra32);
        bitmap.Render(main);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), name));
        encoder.Save(file);
    }
    public static async Task RunAsync(MainWindow main, Action<bool, string> check)
    {
        var cases = new List<object>();
        var startupCases = new List<object>();
        // 가상 작업 영역을 주입해 최초 창 위치/크기가 화면 안에 들어가는지 확인한다.
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
            var invisible = new List<string>();
            int count = 0;
            double sensitivity = main.Model.Sensitivity;
            var draft = main.Model.Expressions.Selected.Draft;
            for (int tab = 0; tab < main.SettingsTabs.Items.Count; tab++)
            {
                main.SettingsTabs.SelectedIndex = tab;
                await Task.Delay(50); main.UpdateLayout();
                var sectionTabs = Descendants(root).OfType<TabControl>().FirstOrDefault(control => !ReferenceEquals(control, main.SettingsTabs));
                int pages = sectionTabs?.Items.Count ?? 1;
                for (int page = 0; page < pages; page++)
                {
                    if (sectionTabs != null) sectionTabs.SelectedIndex = page;
                    await Task.Delay(30); main.UpdateLayout();
                    var controls = Descendants(root).Where(element => element.IsVisible &&
                        (element is Slider or CheckBox or ComboBox or TextBox || element is Button { Command: not null })).ToArray();
                    count += controls.Length;
                    foreach (var control in controls)
                    {
                        string label = control is ContentControl content ? content.Content?.ToString() ?? control.GetType().Name : System.Windows.Automation.AutomationProperties.GetName(control);
                        bool visible = FullyVisible(control, root);
                        check(visible, $"layout_tab_{tab}_{width}x{height}_{label}");
                        if (!visible) invisible.Add(label);
                    }
                    check(Descendants(root).OfType<TabItem>().Where(item => item.IsVisible).All(item => FullyVisible(item, root)), $"layout_tab_headers_{width}x{height}_{tab}_{page}");
                    check(Descendants(root).OfType<ScrollViewer>().Where(scroll => scroll.IsVisible).All(scroll => scroll.ScrollableHeight == 0 && scroll.ScrollableWidth == 0), $"layout_no_content_scroll_{width}x{height}_{tab}_{page}");
                    foreach (var combo in controls.OfType<ComboBox>())
                    {
                        if (combo.Items.Count == 0)
                        {
                            main.Model.Devices.Add(new AudioDevice("qa-synthetic", "QA 마이크 · 긴 장치 이름"));
                            main.Model.SelectedDevice = main.Model.Devices[0]; main.UpdateLayout();
                        }
                        check(combo.Foreground is SolidColorBrush foreground && foreground.Color == Colors.Black &&
                            Descendants(combo).OfType<TextBlock>().Where(text => text.IsVisible && !string.IsNullOrWhiteSpace(text.Text)).All(text => text.Foreground is SolidColorBrush brush && brush.Color == Colors.Black), $"layout_select_black_{width}x{height}_{tab}_{page}");
                        combo.IsDropDownOpen = true; await Task.Delay(30); combo.UpdateLayout();
                        var item = combo.ItemContainerGenerator.ContainerFromIndex(0) as ComboBoxItem;
                        check(item != null && item.Foreground is SolidColorBrush itemBrush && itemBrush.Color == Colors.Black &&
                            Descendants(item).OfType<TextBlock>().Where(text => text.IsVisible).All(text => text.Foreground is SolidColorBrush brush && brush.Color == Colors.Black), $"layout_select_popup_black_{width}x{height}_{tab}_{page}");
                        combo.IsDropDownOpen = false;
                    }
                    Save(main, $"voxpet-layout-{width}x{height}-tab-{tab}-{page}.png", 1);
                }
            }
            check(main.Model.Sensitivity == sensitivity && main.Model.Expressions.Selected.Draft == draft, $"layout_tab_switch_preserves_edit_{width}x{height}");
            main.SettingsTabs.SelectedIndex = 0; main.UpdateLayout();
            if (width >= 870 && height >= 600)
                check(FullyVisible(main.PreviewCard, root), $"layout_preview_card_at_top_{width}x{height}");
            // 래스터 렌더 배율만 바꾼 증거 PNG다. 실제 OS DPI나 다중 모니터 이동 결과로 기록하지 않는다.
            foreach (double scale in new[] { 1.0, 1.5, 2.0 })
            {
                Save(main, $"voxpet-layout-{width}x{height}-{scale:F1}.png", scale);
            }
            cases.Add(new { requestedWidth = width, requestedHeight = height, actualWidth = main.ActualWidth, actualHeight = main.ActualHeight, controls = count, invisible, tabs = main.SettingsTabs.Items.Count, contentScroll = false });
        }
        main.Width = 1000; main.Height = 730; await Task.Delay(100); main.UpdateLayout();
        File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), "voxpet-layout-result.json"), JsonSerializer.Serialize(new { syntheticRenderScale = true, physicalDpiChange = false, startupCases, cases }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
