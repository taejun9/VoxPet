using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;

namespace VoxPet.App.Views;

/// <summary>실제 Windows에서 슬롯 저장/재시작, 전환, 무음 모션과 native 전역 키 수명을 검증한다.</summary>
internal static class ExpressionQa
{
    [DllImport("user32.dll")]
    private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    private static void Hotkey(byte key)
    {
        try
        {
            keybd_event(0x11, 0, 0, UIntPtr.Zero); keybd_event(0x10, 0, 0, UIntPtr.Zero); keybd_event(key, 0, 0, UIntPtr.Zero);
        }
        finally
        {
            keybd_event(key, 0, 2, UIntPtr.Zero); keybd_event(0x10, 0, 2, UIntPtr.Zero); keybd_event(0x11, 0, 2, UIntPtr.Zero);
        }
    }
    private static void SaveWindow(Window window, string name)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), name));
        encoder.Save(file);
    }
    public static async Task RunAsync(MainWindow main, Action<bool, string> check)
    {
        var character = main.Model.Character;
        await main.Model.Expressions.ActivateAsync(2); await Task.Delay(300);
        check(character.Name.Contains("슬픔") && character.Blend == 1, "expression_sad_transition_settles");
        double initial = character.TearLeftY; bool tearMoves = false, blinkMoves = false;
        for (int i = 0; i < 325; i++)
        {
            await Task.Delay(20); tearMoves |= Math.Abs(initial - character.TearLeftY) > 10 && character.TearLeftOpacity > .2;
            blinkMoves |= character.Sprite is BitmapImage image && image.UriSource.ToString().Contains("-blink");
        }
        check(tearMoves && blinkMoves && main.Model.VoiceLevel == 0, "expression_tears_and_blink_without_microphone");
        main.UpdateLayout(); SaveWindow(main, "voxpet-expression-preview.png");
        var broadcast = main.ShowCharacter(); broadcast.UpdateLayout(); SaveWindow(broadcast, "voxpet-expression-broadcast.png");
        main.Model.Expressions.Selected.Blink = false;
        await main.Model.Expressions.PreviewAsync();
        character.Update(new(0, 0, 0, 0, 0, MouthState.Closed));
        check(character.Sprite is BitmapImage awake && !awake.UriSource.ToString().Contains("-blink"), "expression_blink_can_be_disabled");
        main.Model.Expressions.Selected.Blink = true;
        await main.Model.Expressions.ActivateAsync(0);
        var transition = new CharacterViewModel();
        transition.Update(new(0, 0, 0, 0, 1, MouthState.Closed), 100);
        transition.Apply(ExpressionProfile.Default(1));
        transition.Update(new(0, 0, 0, 0, 1, MouthState.Closed), 100.12);
        check(Math.Abs(transition.Blend - .5) < .001 && transition.PreviousSprite is { IsFrozen: true }, "expression_transition_midpoint");
        transition.Apply(ExpressionProfile.Default(3));
        check(transition.Blend == 0 && transition.PreviousSprite is RenderTargetBitmap { PixelWidth: 512 }, "expression_interrupted_transition_flattens");
        transition.Update(new(0, 0, 0, 0, 1, MouthState.Closed), 100.4);
        check(transition.Blend == 1 && transition.PreviousSprite == null, "expression_interrupted_transition_settles");
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-expression-qa-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        var editor = new ExpressionViewModel(character, persistent: true, folder);
        try
        {
            await editor.InitializeAsync(); editor.Selected = editor.Slots[11];
            editor.Selected.Name = "QA 눈물"; editor.Selected.Kind = ExpressionKind.Sad; editor.Selected.Tears = true;
            editor.Selected.TearLeft = .3; editor.Selected.TearRight = .7; editor.Selected.TearTop = .4;
            await editor.SaveAsync();
            var restart = new ExpressionViewModel(character, persistent: true, folder); await restart.InitializeAsync();
            check(restart.Slots[11].Saved == editor.Slots[11].Saved, "expression_slots_survive_restart");
            string path = Path.Combine(folder, "source.png");
            var pixels = new byte[384 * 256 * 4];
            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 3; col++)
                    for (int y = 20; y < 116; y++)
                        for (int x = 20; x < 108; x++)
                        {
                            int offset = ((row * 128 + y) * 384 + col * 128 + x) * 4;
                            pixels[offset] = (byte)(40 + col * 70); pixels[offset + 1] = (byte)(row == 0 ? 70 : 180); pixels[offset + 3] = 255;
                        }
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(384, 256, 96, 96, PixelFormats.Bgra32, null, pixels, 384 * 4)));
            using (var file = File.Create(path)) encoder.Save(file);
            check(await editor.ImportAsync(path), "expression_custom_sheet_import");
            await editor.SaveAsync(); File.Delete(path);
            await restart.CloseAsync(); restart = new ExpressionViewModel(character, persistent: true, folder); await restart.InitializeAsync();
            await restart.ActivateAsync(11);
            check(character.Name.Contains("QA 눈물") && character.Sprite is BitmapSource { PixelWidth: 128 }, "expression_owned_copy_survives_original_removal");
            var customSprite = character.Sprite;
            File.WriteAllText(Path.Combine(folder, restart.Slots[11].Saved.SheetId + ".png"), "corrupt");
            await restart.ActivateAsync(11);
            check(ReferenceEquals(customSprite, character.Sprite) && restart.Status.Contains("현재 표정을 유지"), "expression_corrupt_sheet_preserves_display");
            // 앞선 손상 시트 요청과 뒤의 정상 전환을 동시에 큐에 넣어 최신 요청 우선 계약을 확인한다.
            var requests = new List<Task> { restart.ActivateAsync(11) };
            for (int i = 0; i < 30; i++) requests.Add(restart.ActivateAsync(i % 6));
            requests.Add(restart.ActivateAsync(4)); await Task.WhenAll(requests);
            check(character.Name.Contains("놀람"), "expression_latest_request_wins");
            restart.Selected = restart.Slots[11]; restart.BuiltinCommand.Execute(null); await restart.SaveAsync();
            check(restart.Slots[11].Saved.SheetId == null && Directory.GetFiles(folder, "*.png").Length == 0, "expression_builtin_replaces_corrupt_sheet");
            await restart.CloseAsync();
        }
        finally { await editor.CloseAsync(); Directory.Delete(folder, true); }
        // 실제 RegisterHotKey/WM_HOTKEY + 합성 키 입력이다. 물리 키보드 실기와 구분한다.
        using var probe = new HwndSource(new HwndSourceParameters("VoxPet key QA") { Width = 100, Height = 100 });
        var hotkeys = main.EnableExpressionHotkeys();
        check(hotkeys.Registered.Count == 11 && !hotkeys.Registered.Contains(11), "expression_global_F1_F11_F12_reserved");
        using (var collision = new ExpressionHotkeys(probe, _ => { }))
            check(collision.Registered.Count == 0 && collision.Conflicts.Count == 11, "expression_global_conflicts_reported");
        main.WindowState = WindowState.Minimized; await Task.Delay(100);
        Hotkey(0x72); await Task.Delay(300);
        check(character.Name.Contains("슬픔"), "expression_global_key_while_minimized");
        main.WindowState = WindowState.Normal;
        check(main.Model.HandleExpressionKey(Key.F12, ModifierKeys.Control | ModifierKeys.Shift, false), "expression_F12_local_fallback");
        await Task.Delay(100);
        check(character.Name.Contains("표정 12"), "expression_F12_selects_slot_12");
        check(!main.Model.HandleExpressionKey(Key.F1, ModifierKeys.Control, false), "expression_wrong_modifiers_ignored");
        hotkeys.Dispose();
        using (var reacquired = new ExpressionHotkeys(probe, _ => { }))
            check(reacquired.Registered.Count == 11, "expression_global_keys_released");
        await main.Model.Expressions.ActivateAsync(0);
    }
}
