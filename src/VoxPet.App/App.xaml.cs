using System.Diagnostics;
using System.IO;
using System.Windows;
using VoxPet.App.Views;

namespace VoxPet.App;

public partial class App : Application
{
    private int failures;
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
            await Task.Delay(1200);
            if (main.Model.Character.Sprite.Width <= 0 || !broadcast.IsVisible) failures++;
            main.Model.GreenBackground = false;
            main.Model.Topmost = false;
            await Task.Delay(100);
            main.Model.StopCommand.Execute(null);
            await Task.Delay(100);
            if (main.Model.VoiceLevel != 0) failures++;
            broadcast.Close();
            var png = new System.Windows.Media.Imaging.RenderTargetBitmap(1000, 730, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            png.Render(main);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(png));
            using (var stream = File.Create(Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), "voxpet-smoke.png"))) encoder.Save(stream);
            await main.ShutdownAsync();
            Shutdown(failures == 0 ? 0 : 1);
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), "voxpet-smoke-error.txt"), $"{ex.GetType().Name} HResult={ex.HResult:X8}");
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
