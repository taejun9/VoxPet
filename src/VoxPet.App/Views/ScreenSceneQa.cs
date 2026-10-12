using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;

namespace VoxPet.App.Views;

/// <summary>공용 고양이와 합성 의상만 실제 Windows 창에 표시하고 desktop pixels를 캡처한다.</summary>
internal static class ScreenSceneQa
{
    private static ImageSource Accessory(string kind)
    {
        int width = kind == "sleeves" ? 512 : kind == "hat" ? 160 : 28;
        int height = kind == "sleeves" ? 512 : kind == "hat" ? 42 : 14;
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            if (kind == "sleeves")
            {
                drawing.DrawRoundedRectangle(Brushes.DarkSeaGreen, new Pen(Brushes.DarkGreen, 3), new Rect(156, 310, 88, 55), 16, 16);
                drawing.DrawRoundedRectangle(Brushes.DarkSeaGreen, new Pen(Brushes.DarkGreen, 3), new Rect(270, 310, 88, 55), 16, 16);
                drawing.DrawRoundedRectangle(Brushes.DarkOliveGreen, new Pen(Brushes.DarkGreen, 3), new Rect(212, 345, 88, 70), 10, 10);
            }
            else drawing.DrawRoundedRectangle(kind == "hat" ? Brushes.DeepSkyBlue : Brushes.HotPink, new Pen(Brushes.Gold, 4), new Rect(2, 2, width - 4, height - 4), 5, 5);
        }
        var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); image.Render(visual); image.Freeze(); return image;
    }
    public static async Task RunAsync(Action<bool, string> check)
    {
        int inputCalls = 0;
        var model = new MainViewModel(false, () => [], _ => { inputCalls++; throw new InvalidOperationException(); });
        var main = new MainWindow(true, new Rect(0, 0, 1000, 730), model);
        Action? resume = null;
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-screen-scenes-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        try
        {
            main.Show(); await main.Ready; resume = model.PauseVisualForQa();
            main.SettingsTabs.SelectedIndex = 2; main.Width = 1000; main.Height = 730;
            var broadcast = main.ShowCharacter(); broadcast.Left = 40; broadcast.Top = 60; model.BroadcastWidth = 480; model.BroadcastHeight = 480; model.GreenBackground = true;
            double now = 100;
            foreach (var kind in Enum.GetValues<ExpressionKind>())
            {
                await model.Expressions.PreviewBuiltinAsync((int)kind);
                foreach (var mouth in Enum.GetValues<MouthState>()) foreach (double eye in new[] { 0d, 1d })
                {
                    model.Character.Update(new((int)mouth / 2d, 0, 0, 0, eye, mouth), now++);
                    await ScreenEvidence.CaptureAsync(broadcast, $"expression-{kind.ToString().ToLowerInvariant()}-mouth-{(int)mouth}-eye-{(int)eye}", check,
                        new { expression = kind.ToString(), mouth, eye, publicArtwork = true });
                }
                await ScreenEvidence.CaptureAsync(main, "expression-ui-" + kind.ToString().ToLowerInvariant(), check);
            }
            await model.Expressions.PreviewBuiltinAsync(0);
            foreach (var (kind, category, x, y, width) in new[] { ("sleeves", CostumeCategory.Top, 0d, 0d, 512d), ("hat", CostumeCategory.Hat, 175d, 80d, 160d), ("mouth", CostumeCategory.FaceDecoration, 242d, 286d, 28d) })
            {
                var image = Accessory(kind); string fileName = Path.Combine(folder, "synthetic-" + kind + ".png");
                await Task.Run(() => { var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create((BitmapSource)image)); using var file = File.Create(fileName); encoder.Save(file); });
                model.Wardrobe.ImportCategory = category;
                check(await model.Wardrobe.ImportAsync(fileName), "screen_real_wardrobe_import_" + kind);
                var item = model.Wardrobe.Selected!; item.X = x; item.Y = y; item.SizePercent = width / item.Profile.BaseWidth * 100;
            }
            var rig = CharacterRig.Default("builtin");
            foreach (var gesture in Enum.GetValues<HandGesture>())
                for (int frame = 0; frame < 4; frame++)
                {
                    double progress = new[] { .1, .35, .65, .9 }[frame];
                    model.Character.SetMotion(rig, new(gesture, progress), HandMotionProfile.Default(gesture));
                    model.Character.Update(new(0, 0, 0, 0, 1, MouthState.Closed), now++);
                    await ScreenEvidence.CaptureAsync(broadcast, $"hand-{gesture.ToString().ToLowerInvariant()}-{frame}", check, new { gesture, frame, progress, sleeves = true });
                }
            foreach (double angle in new[] { -6d, 0d, 6d })
            {
                model.Character.SetMotion(rig with { HeadMotion = true }, null, null);
                model.Character.Update(new(0, 0, angle, 0, 1, MouthState.Closed), now++);
                await ScreenEvidence.CaptureAsync(broadcast, "attachment-head-" + (angle + 6).ToString("F0"), check, new { angle, attachment = "head" });
            }
            foreach (double level in new[] { 0d, .5d, 1d })
            {
                model.Character.SetMotion(rig with { HeadMotion = true }, null, null);
                model.Character.Update(new(level, 0, 4, 0, 1, (MouthState)(level * 2)), now++);
                await ScreenEvidence.CaptureAsync(broadcast, "attachment-mouth-" + (level * 2).ToString("F0"), check, new { level, attachment = "mouth-and-head" });
            }
            model.GreenBackground = false; await ScreenEvidence.CaptureAsync(broadcast, "broadcast-transparent", check);
            var handle = new System.Windows.Interop.WindowInteropHelper(broadcast).Handle;
            model.GreenBackground = true; await ScreenEvidence.CaptureAsync(broadcast, "broadcast-green-again", check);
            check(handle == new System.Windows.Interop.WindowInteropHelper(broadcast).Handle, "screen_same_broadcast_hwnd_after_background_switch");
            main.HideToTray(); await ScreenEvidence.CaptureAsync(broadcast, "broadcast-while-settings-in-tray", check);
            main.RestoreFromTray(); await ScreenEvidence.CaptureAsync(main, "settings-restored-from-tray", check);
            check(inputCalls == 0, "screen_scenes_no_microphone_input");
        }
        finally { resume?.Invoke(); await main.ShutdownAsync(); main.Close(); Directory.Delete(folder, true); }
    }
}
