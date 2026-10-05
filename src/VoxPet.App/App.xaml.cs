using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using VoxPet.App.Views;

namespace VoxPet.App;

public partial class App : Application
{
    private int failures;
    private readonly List<string> failedChecks = [];
    private static string QaPath(string name) => Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), name);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr window);
    private void Check(bool passed, string name)
    {
        if (passed) return;
        failures++; failedChecks.Add(name);
    }
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--smoke-test")) RunSmokeAsync();
        else { var main = new MainWindow(); MainWindow = main; main.Show(); }
    }
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
            var closingWindow = new MainWindow(smoke: true); closingWindow.Show();
            await closingWindow.Ready; closingWindow.Close();
            await Task.Delay(100);
            if (closingWindow.IsVisible) failures++;
            var png = new System.Windows.Media.Imaging.RenderTargetBitmap(1000, 730, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            png.Render(main);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(png));
            using (var stream = File.Create(QaPath("voxpet-smoke.png"))) encoder.Save(stream);
            await main.ShutdownAsync();
            Check(!reopened.IsVisible, "broadcast_closed_on_main_shutdown");
            File.WriteAllText(QaPath("voxpet-smoke-result.json"), JsonSerializer.Serialize(new { failures, failedChecks }, new JsonSerializerOptions { WriteIndented = true }));
            Shutdown(failures == 0 ? 0 : 1);
        }
        catch (Exception ex)
        {
            File.WriteAllText(QaPath("voxpet-smoke-error.txt"), $"{ex.GetType().Name} HResult={ex.HResult:X8}");
            Shutdown(1);
        }
        finally { PresentationTraceSources.DataBindingSource.Listeners.Remove(listener); listener.Dispose(); }
    }
    private sealed class BindingErrorListener(Action error) : TraceListener
    {
        public override void Write(string? message) { if (!string.IsNullOrWhiteSpace(message)) error(); }
        public override void WriteLine(string? message) => Write(message);
    }
}
