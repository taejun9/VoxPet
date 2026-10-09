using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using VoxPet.App.Views;

namespace VoxPet.App;

/// <summary>
/// 일반 앱 실행과 마이크 없는 Windows QA 진입점을 분리한다. QA는 화면/숫자만 시험한다.
/// </summary>
public partial class App : Application
{
    private int failures;
    private readonly List<string> failedChecks = [];
    private readonly HashSet<string> passedChecks = [];
    // QA 증거를 runner temp에 기록해 사용자 문서나 영구 설정을 변경하지 않는다.
    private static string QaPath(string name) => Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), name);
    // WPF 속성뿐 아니라 native 창의 가시성·최소화·resize hit test도 smoke에서 확인한다.
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    // WPF visual tree에서 resize grip을 찾아 픽셀은 숨겨도 native 조작 영역이 남는지 검사한다.
    private static ResizeGrip? FindResizeGrip(DependencyObject element)
    {
        if (element is ResizeGrip grip) return grip;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            var found = FindResizeGrip(VisualTreeHelper.GetChild(element, i));
            if (found != null) return found;
        }
        return null;
    }
    private void Check(bool passed, string name)
    {
        if (passed) passedChecks.Add(name);
        if (passed) return;
        failures++; failedChecks.Add(name);
    }
    /// <summary>
    /// 일반 실행은 설정창을 연다. --smoke-test와 --qa-demo는 자동 검증 전용 인수다.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--smoke-test")) RunSmokeAsync();
        else if (e.Args.Contains("--qa-demo")) RunQaDemoAsync(e.Args.Contains("--qa-transparent"));
        else { var main = new MainWindow(); MainWindow = main; main.Show(); }
    }
    // OBS QA fixture: 합성 숫자 데모만 사용한다. 마이크를 시작하거나 개인 설정을 저장하지 않는다.
    private async void RunQaDemoAsync(bool transparent)
    {
        try
        {
            var main = new MainWindow(smoke: true); MainWindow = main; main.Show();
            await main.Ready;
            main.Model.GreenBackground = !transparent;
            main.Model.DemoCommand.Execute(null);
            main.ShowCharacter();
            main.WindowState = WindowState.Minimized;
        }
        catch (Exception ex)
        {
            File.WriteAllText(QaPath("voxpet-qa-error.txt"), ex.GetType().Name);
            Shutdown(1);
        }
    }
    /// <summary>
    /// 실제 Windows WPF에서 바인딩·캐릭터 반응·방송창 수명·PNG·배치를 검사한다.
    /// Task.Delay로 Dispatcher가 처리할 시간을 주며 실패 수와 PNG를 기록하고 종료 코드로 QA에 알린다.
    /// </summary>
    private async void RunSmokeAsync()
    {
        var listener = new BindingErrorListener(() => failures++);
        PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        try
        {
            var main = new MainWindow(smoke: true); MainWindow = main; main.Show();
            await main.Ready;
            main.Model.DemoCommand.Execute(null);
            var broadcast = main.ShowCharacter();
            bool reacted = false, blinked = false;
            var previewClock = Stopwatch.StartNew();
            while (previewClock.Elapsed.TotalSeconds < 6.5)
            {
                await Task.Delay(20);
                reacted |= main.Model.VoiceLevel >= .2;
                blinked |= main.Model.Character.Sprite is System.Windows.Media.Imaging.BitmapImage bitmap && bitmap.UriSource.ToString().Contains("-blink");
                if (!double.IsFinite(main.Model.VoiceLevel) || main.Model.VoiceLevel is < 0 or > 1) failures++;
            }
            if (!reacted || !blinked) failures++;
            if (main.Model.Character.Sprite.Width <= 0 || !broadcast.IsVisible) failures++;
            // WM_NCHITTEST(0x0084)의 HTBOTTOMRIGHT(17) 결과로 실제 우하단 크기 조절 영역을 확인한다.
            var grip = FindResizeGrip(broadcast);
            Check(grip is { IsVisible: true, Opacity: 0 } && grip.ActualWidth > 0 && grip.ActualHeight > 0, "broadcast_grip_invisible_and_active");
            if (grip != null)
            {
                var point = grip.PointToScreen(new Point(grip.ActualWidth / 2, grip.ActualHeight / 2));
                var packed = new IntPtr(((int)point.X & 0xffff) | (((int)point.Y & 0xffff) << 16));
                Check(SendMessage(new WindowInteropHelper(broadcast).Handle, 0x0084, IntPtr.Zero, packed).ToInt64() == 17, "broadcast_native_resize_hit");
            }
            main.Model.BroadcastWidth = 600; main.Model.BroadcastHeight = 360;
            await Task.Delay(50);
            Check(broadcast.ActualWidth == 600 && broadcast.ActualHeight == 360, "broadcast_size_controls_apply");
            broadcast.SetCurrentValue(Window.WidthProperty, 520.0); broadcast.SetCurrentValue(Window.HeightProperty, 420.0); await Task.Delay(50);
            Check(main.Model.BroadcastWidth == 520 && main.Model.BroadcastHeight == 420, "broadcast_resize_updates_controls");
            main.Model.BroadcastWidth = double.NaN; main.Model.BroadcastHeight = 150;
            Check(main.Model.BroadcastWidth == 480 && main.Model.BroadcastHeight == 200, "broadcast_size_bounds_finite");
            main.Model.BroadcastWidth = 480; main.Model.BroadcastHeight = 480;
            main.Model.NormalizeMin = -80; main.Model.NormalizeMax = -20;
            main.Model.NormalizeMin = -1;
            if (main.Model.NormalizeMin >= main.Model.NormalizeMax) failures++;
            main.Model.NormalizeMax = -119;
            if (main.Model.NormalizeMin >= main.Model.NormalizeMax) failures++;
            main.Model.ResetCommand.Execute(null);
            if (main.Model.NormalizeMin != -50 || main.Model.NormalizeMax != -10) failures++;
            main.Model.GreenBackground = false;
            main.Model.Topmost = false;
            await Task.Delay(100);
            // 설정창 최소화가 독립 방송창에 전파되는 회귀를 native 창 상태로 확인한다.
            main.WindowState = WindowState.Minimized;
            await Task.Delay(200);
            var mainHandle = new WindowInteropHelper(main).Handle;
            var broadcastHandle = new WindowInteropHelper(broadcast).Handle;
            Check(IsIconic(mainHandle), "main_minimize_exercised");
            Check(IsWindowVisible(broadcastHandle) && !IsIconic(broadcastHandle), "broadcast_survives_main_minimize");
            main.WindowState = WindowState.Normal;
            await Task.Delay(100);
            broadcast.WindowState = WindowState.Minimized;
            await Task.Delay(100);
            var restored = main.ShowCharacter();
            await Task.Delay(100);
            Check(ReferenceEquals(restored, broadcast) && IsWindowVisible(broadcastHandle) && !IsIconic(broadcastHandle), "broadcast_open_restores_minimized_window");
            main.Model.StopCommand.Execute(null);
            await Task.Delay(100);
            if (main.Model.VoiceLevel != 0) failures++;
            broadcast.Close();
            var reopened = main.ShowCharacter();
            Check(!ReferenceEquals(broadcast, reopened) && IsWindowVisible(new WindowInteropHelper(reopened).Handle), "broadcast_reopens");
            Check(ReferenceEquals(reopened.DataContext, main.Model), "broadcast_shares_model");
            Check(reopened.ActualWidth == main.Model.BroadcastWidth && reopened.ActualHeight == main.Model.BroadcastHeight, "broadcast_reopens_at_selected_size");
            var closingWindow = new MainWindow(smoke: true); closingWindow.Show();
            await closingWindow.Ready; closingWindow.Close();
            await Task.Delay(100);
            if (closingWindow.IsVisible) failures++;
            await CharacterQa.RunAsync(main, Check);
            await ExpressionQa.RunAsync(main, Check);
            await DetailedCharacterQa.RunAsync(Check);
            await ExpressionManagementQa.RunAsync(main, Check);
            await UsabilityQa.RunAsync(Check);
            await LayoutQa.RunAsync(main, Check);
            var png = new System.Windows.Media.Imaging.RenderTargetBitmap(1000, 730, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            png.Render(main);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(png));
            using (var stream = File.Create(QaPath("voxpet-smoke.png"))) encoder.Save(stream);
            await main.ShutdownAsync();
            Check(!reopened.IsVisible, "broadcast_closed_on_main_shutdown");
            File.WriteAllText(QaPath("voxpet-smoke-result.json"), JsonSerializer.Serialize(new { failures, failedChecks, passedChecks = passedChecks.Order().ToArray() }, new JsonSerializerOptions { WriteIndented = true }));
            Shutdown(failures == 0 ? 0 : 1);
        }
        catch (Exception ex)
        {
            File.WriteAllText(QaPath("voxpet-smoke-error.txt"), $"{ex.GetType().Name} HResult={ex.HResult:X8}");
            Shutdown(1);
        }
        finally { PresentationTraceSources.DataBindingSource.Listeners.Remove(listener); listener.Dispose(); }
    }
    // 개인 값이 포함될 수 있는 바인딩 메시지는 기록하지 않고 오류 발생만 집계한다.
    private sealed class BindingErrorListener(Action error) : TraceListener
    {
        public override void Write(string? message) { if (!string.IsNullOrWhiteSpace(message)) error(); }
        public override void WriteLine(string? message) => Write(message);
    }
}
