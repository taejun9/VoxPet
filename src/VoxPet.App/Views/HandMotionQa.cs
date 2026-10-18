using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;
using VoxPet.Core.Triggers;
using VoxPet.Core.Services;

namespace VoxPet.App.Views;

/// <summary>합성 PNG/설정/입력만 사용. 실제 마이크나 개인 캐릭터를 읽지 않는다.</summary>
internal static class HandMotionQa
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput { public int X, Y; public uint Data, Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Input { public uint Type; public MouseInput Mouse; }
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, Input[] inputs, int size);
    private static uint InjectClick() => SendInput(2, [new() { Mouse = new() { Flags = 2 } }, new() { Mouse = new() { Flags = 4 } }], Marshal.SizeOf<Input>());
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string name);
    private static async Task<bool> OwnerReadyAsync(HwndSource source)
    {
        int message = (int)RegisterWindowMessage("VoxPet.Qa.HandInputReady");
        if (message == 0) return false;
        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        IntPtr Hook(IntPtr window, int id, IntPtr wParam, IntPtr lParam, ref bool handled)
        { if (id == message) { handled = true; ready.TrySetResult(true); } return IntPtr.Zero; }
        source.AddHook(Hook);
        try
        {
            if (!PostMessage(source.Handle, message, IntPtr.Zero, IntPtr.Zero)) return false;
            return await ready.Task.WaitAsync(TimeSpan.FromSeconds(1));
        }
        catch (TimeoutException) { return false; }
        finally { source.RemoveHook(Hook); }
    }
    private static ImageSource Markers()
    {
        var pixels = new byte[512 * 512 * 4];
        void Square(int x, int y, byte r, byte g, byte b)
        {
            for (int row = y; row < y + 12; row++) for (int column = x; column < x + 12; column++)
            { int i = (row * 512 + column) * 4; pixels[i] = b; pixels[i + 1] = g; pixels[i + 2] = r; pixels[i + 3] = 255; }
        }
        Square(98, 392, 0, 255, 0); Square(250, 370, 0, 255, 255); Square(120, 100, 255, 0, 0);
        var bitmap = BitmapSource.Create(512, 512, 96, 96, PixelFormats.Bgra32, null, pixels, 2048); bitmap.Freeze(); return bitmap;
    }
    private static byte[] Render(CharacterViewModel model, string? name = null, bool props = true)
    {
        var view = new MotionSurface { DataContext = model, Width = 512, Height = 512, DrawMotionProps = props };
        view.Measure(new Size(512, 512)); view.Arrange(new Rect(0, 0, 512, 512)); view.UpdateLayout();
        var bitmap = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32); bitmap.Render(view);
        if (name != null)
        {
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), name)); encoder.Save(file);
        }
        byte[] pixels = new byte[512 * 512 * 4]; bitmap.CopyPixels(pixels, 2048, 0); return pixels;
    }
    private static Point Centroid(byte[] pixels, Func<byte, byte, byte, bool> color)
    {
        double x = 0, y = 0, count = 0;
        for (int row = 0; row < 512; row++) for (int column = 0; column < 512; column++)
        {
            int i = (row * 512 + column) * 4;
            if (pixels[i + 3] > 220 && color(pixels[i + 2], pixels[i + 1], pixels[i])) { x += column; y += row; count++; }
        }
        return count == 0 ? new(-1, -1) : new(x / count, y / count);
    }
    public static async Task RunAsync(MainWindow main, Action<bool, string> check, int nativeRounds = 20)
    {
        if (nativeRounds is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(nativeRounds));
        check(main.SettingsTabs.Items.Count == 7, "motion_settings_7_top_level_tabs");
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-motion-qa-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        var character = new CharacterViewModel(); var hands = new HandMotionViewModel(character, true, folder, () => 1);
        try
        {
            await hands.InitializeAsync(); hands.SelectedIndex = 2; hands.InputIndex = 1; hands.Enabled = true; hands.ModeIndex = 1; hands.Interval = 7.5;
            hands.SetCharacter("pelly"); hands.HeadMotion = true; hands.MouthTravel = 23; hands.SideIndex = 1; hands.LimbX = 308;
            await Task.Delay(500);
            var saved = new HandMotionStore(Path.Combine(folder, "motion.json")).Load().Settings;
            check(saved.Profiles[2].Enabled && saved.Profiles[2].IntervalSeconds == 7.5 && saved.Rigs!["pelly"].HeadMotion && saved.Rigs["pelly"].Right.X == 308, "motion_auto_save_modes_and_character_rig");
            var priorBytes = File.ReadAllBytes(Path.Combine(folder, "motion.json"));
            var earlyClose = new HandMotionViewModel(character, true, folder, () => 1); await earlyClose.CloseAsync(); await earlyClose.InitializeAsync();
            check(priorBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(folder, "motion.json"))), "motion_close_before_load_preserves_last_settings");
            hands.X = 25; await hands.CloseAsync();
            var restart = new HandMotionViewModel(character, true, folder, () => 1); await restart.InitializeAsync(); restart.SelectedIndex = 2; restart.SetCharacter("pelly");
            check(restart.X == 25 && restart.HeadMotion && restart.MouthTravel == 23 && !restart.Rig.Left.Enabled, "motion_restart_last_edit_flush_and_side_pelly");
            restart.SetCharacter("neulbo"); check(!restart.HeadMotion && restart.Rig.Left.Enabled, "motion_character_specific_rigs"); await restart.CloseAsync();
            var rig = CharacterRig.Default("neulbo");
            var marker = new CostumeItemViewModel(new(Guid.NewGuid(), "marker", CostumeCategory.Top, 0, 0, 512, 512, Attachment: MotionAttachment.Garment), (_, _) => { }) { Image = Markers() };
            character.CostumeLayers.Add(marker); character.Update(new(0, 0, 0, 0, 1, MouthState.Closed), 1);
            var before = Render(character); var greenBefore = Centroid(before, (r, g, b) => r < 20 && g > 230 && b < 20);
            var cyanBefore = Centroid(before, (r, g, b) => r < 20 && g > 230 && b > 230);
            var profile = HandMotionProfile.Default(HandGesture.Heart);
            character.SetMotion(rig, new(HandGesture.Heart, .5), profile);
            var after = Render(character, "voxpet-motion-garment.png");
            var greenAfter = Centroid(after, (r, g, b) => r < 20 && g > 230 && b < 20);
            var cyanAfter = Centroid(after, (r, g, b) => r < 20 && g > 230 && b > 230);
            var expected = MotionSurface.AttachmentTransform(character, MotionAttachment.LeftHand, 1).Transform(greenBefore);
            check(greenBefore.X > 0 && greenAfter.X > 0 && (greenAfter - expected).Length < 1 && (greenAfter - greenBefore).Length > 5 && (cyanAfter - cyanBefore).Length < .1, "motion_sleeve_follows_hand_body_stays_aligned");
            marker.AttachmentIndex = (int)MotionAttachment.Head; character.SetMotion(rig with { HeadMotion = true }, null, null);
            character.Update(new(0, 0, 6, 0, 1, MouthState.Closed), 1);
            var head = Render(character, "voxpet-motion-head.png"); var red = Centroid(head, (r, g, b) => r > 230 && g < 20 && b < 20);
            var expectedHead = MotionSurface.AttachmentTransform(character, MotionAttachment.Head, 1).Transform(new Point(125.5, 105.5));
            check(red.X > 0 && (red - expectedHead).Length < 1, "motion_head_decoration_uses_head_pivot");
            marker.AttachmentIndex = (int)MotionAttachment.Mouth; character.Update(new(1, 0, 6, 0, 1, MouthState.Open), 1);
            var mouth = Render(character, "voxpet-motion-mouth.png"); var redMouth = Centroid(mouth, (r, g, b) => r > 230 && g < 20 && b < 20);
            var expectedMouth = MotionSurface.AttachmentTransform(character, MotionAttachment.Mouth, 1).Transform(new Point(125.5, 105.5));
            check((redMouth - expectedMouth).Length < 1 && (redMouth - red).Length > 9, "motion_mouth_decoration_inherits_head_and_mouth");
            marker.FollowPercent = 0;
            check((Centroid(Render(character), (r, g, b) => r > 230 && g < 20 && b < 20) - new Point(125.5, 105.5)).Length < .1, "motion_zero_follow_preserves_placement");
            character.CostumeLayers.Clear(); character.Update(new(0, 0, 0, 0, 1, MouthState.Closed), 3);
            rig = CharacterRig.Default("builtin");
            foreach (var gesture in Enum.GetValues<HandGesture>())
            {
                character.SetMotion(rig, null, null); var rest = Render(character); var sprite = character.Sprite;
                character.SetMotion(rig, new(gesture, .5), HandMotionProfile.Default(gesture));
                var frame = Render(character, "voxpet-motion-" + gesture + ".png");
                check(!rest.SequenceEqual(frame) && rest.Take(512 * (int)rig.HeadCutY * 4).SequenceEqual(Render(character, props: false).Take(512 * (int)rig.HeadCutY * 4)) && ReferenceEquals(sprite, character.Sprite), "motion_" + gesture + "_hand_changes_face_preserved");
            }
            double testTime = 10;
            var memory = new HandMotionViewModel(character, false, Path.Combine(folder, "memory"), () => testTime); await memory.InitializeAsync(); memory.SetCharacter("builtin");
            memory.Trigger(HandGesture.Claw, true); testTime += .5; memory.Update(testTime); var active = character.Motion;
            memory.SetCharacter("builtin"); check(character.Motion.Gesture == HandGesture.Claw && character.Motion.LeftAngle == active.LeftAngle, "motion_same_character_expression_does_not_cancel_hand");
            memory.SetCharacter("pelly"); check(character.Motion.Gesture == null && !character.Rig.Left.Enabled, "motion_changed_character_cancels_old_hand");
            await memory.CloseAsync(); check(!Directory.Exists(Path.Combine(folder, "memory")), "motion_memory_fixture_no_write");
            var capture = character.CaptureAppearance(); bool custom = character.UsesCustomSheet;
            var customSheet = new ImageSource[3, 2]; for (int mouthIndex = 0; mouthIndex < 3; mouthIndex++) for (int eye = 0; eye < 2; eye++) customSheet[mouthIndex, eye] = Markers();
            character.UseSheet(customSheet, "custom"); check(character.UsesCustomSheet, "motion_custom_sheet_identity"); capture(); check(character.UsesCustomSheet == custom, "motion_test_restore_preserves_asset_identity");
            check(ReferenceEquals(main.ShowCharacter().DataContext, main.Model), "motion_broadcast_keeps_shared_model");
            var source = HwndSource.FromHwnd(new WindowInteropHelper(main).Handle); int hits = 0, clicks = 0;
            var firstStages = new List<int>(); var firstNative = new List<object>();
            using (var inputs = new HandMotionInputs(source, _ => hits++, () => clicks++, firstStages.Add, (code, message) => { if (firstNative.Count < 256) firstNative.Add(new { code, message }); }))
            {
                var signals = new List<TriggerEvent>(); inputs.Triggered += signals.Add;
                var keys = HandMotionSettings.Default() with { Profiles = HandMotionSettings.Default().Profiles.Select(p => p with { LeftClick = false }).ToArray() };
                inputs.Configure(keys); check(inputs.Registered.Count == 1 && inputs.Conflicts.Count == 0, "motion_native_key_registration");
                using (var otherSource = new HwndSource(new HwndSourceParameters("VoxPet-motion-conflict") { Width = 1, Height = 1, WindowStyle = 0 }))
                using (var conflict = new HandMotionInputs(otherSource, _ => { }, () => { })) { conflict.Configure(keys); check(conflict.Conflicts.Count == 1, "motion_native_key_conflict_reported"); }
                PostMessage(source.Handle, 0x0312, new(0x5300), new(0x31 << 16 | 3)); await Task.Delay(50);
                check(hits == 1, "motion_native_wm_hotkey_dispatch");
                check(signals.Count == 1 && signals[0].Kind == TriggerKind.Hotkey && signals[0].Key == "1" &&
                    signals[0].Modifiers == (TriggerModifiers.Ctrl | TriggerModifiers.Alt) && signals[0].IsPulse, "motion_platform_neutral_hotkey_pulse");
                inputs.Configure(HandMotionSettings.Default()); check(inputs.Registered.Count == 0 && inputs.MouseRegistered, "motion_native_switch_to_mouse_releases_key");
                // A neutral same-process surface prevents a synthetic click from
                // hitting an unrelated button left under the cursor by editor QA.
                var inputTarget = new Window
                {
                    Width = 160,
                    Height = 100,
                    Left = SystemParameters.WorkArea.Left + 16,
                    Top = SystemParameters.WorkArea.Top + 16,
                    WindowStyle = WindowStyle.None,
                    ResizeMode = ResizeMode.NoResize,
                    ShowInTaskbar = false,
                    Topmost = true,
                    Background = Brushes.White,
                    Content = new System.Windows.Controls.TextBlock { Text = "VoxPet input QA" }
                };
                try
                {
                    int targetDowns = 0;
                    inputTarget.PreviewMouseDown += (_, e) => { if (e.ChangedButton == System.Windows.Input.MouseButton.Left) targetDowns++; };
                    inputTarget.Show(); inputTarget.UpdateLayout();
                    var point = inputTarget.PointToScreen(new Point(80, 60));
                    check(SetCursorPos((int)point.X, (int)point.Y), "motion_native_mouse_neutral_target");
                    var deliveries = new List<object>();
                    for (int round = 1; round <= nativeRounds; round++)
                    {
                        var stages = round == 1 ? firstStages : new List<int>();
                        var native = round == 1 ? firstNative : new List<object>();
                        using var probe = round == 1 ? inputs : new HandMotionInputs(source, _ => hits++, () => clicks++, stages.Add, (code, message) => { if (native.Count < 256) native.Add(new { code, message }); });
                        if (round > 1) { probe.Triggered += signals.Add; probe.Configure(HandMotionSettings.Default()); }
                        string suffix = round == 1 ? "" : "_round" + round;
                        bool ownerReady = await OwnerReadyAsync(source);
                        int initialClicks = clicks, initialSignals = signals.Count, initialTargetDowns = targetDowns;
                        bool registeredBefore = probe.MouseRegistered;
                        int registrationError = registeredBefore ? 0 : Marshal.GetLastPInvokeError();
                        check(registeredBefore, "motion_native_mouse_hook_registered" + suffix);
                        check(ownerReady, "motion_native_mouse_owner_ready" + suffix);
                        var delivery = Stopwatch.StartNew();
                        uint inserted = InjectClick(); int injectionError = inserted == 2 ? 0 : Marshal.GetLastPInvokeError();
                        check(inserted == 2, "motion_native_mouse_injection_accepted" + suffix);
                        await Task.Delay(50);
                        int clicksAt50 = clicks, signalsAt50 = signals.Count;
                        // The hook posts an Input operation; a Normal-priority timer can
                        // resume first. Wait for this one injected event, never resend it.
                        while (clicks == initialClicks && delivery.Elapsed < TimeSpan.FromMilliseconds(500))
                        {
                            await source.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                            if (clicks == initialClicks) await Task.Delay(10);
                        }
                        await source.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                        double deliveryMs = delivery.Elapsed.TotalMilliseconds;
                        check(clicks - initialClicks == 1, "motion_native_left_down_signal" + suffix);
                        check(signals.Count - initialSignals == 1 && signals[^1].Kind == TriggerKind.Mouse && signals[^1].Button == "left", "motion_platform_neutral_mouse_pulse" + suffix);
                        int clicksBeforeDispose = clicks, signalsBeforeDispose = signals.Count, targetDownsBeforeDispose = targetDowns;
                        probe.Dispose(); uint insertedAfterDispose = InjectClick();
                        check(insertedAfterDispose == 2, "motion_native_disposed_mouse_injection_accepted" + suffix);
                        await Task.Delay(100);
                        await source.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                        check(clicks == clicksBeforeDispose && clicks - initialClicks == 1 && !probe.MouseRegistered, "motion_native_dispose_stops_mouse" + suffix);
                        check(signals.Count == signalsBeforeDispose && signals.Count - initialSignals == 1, "motion_platform_neutral_dispose_stops_events" + suffix);
                        deliveries.Add(new { round, ownerReady, stages = stages.ToArray(), native = native.ToArray(), initialTargetDowns, targetDownsBeforeDispose, targetDownsAfterDispose = targetDowns, initialClicks, initialSignals, registeredBefore, registrationError, inserted, injectionError, insertedAfterDispose, clicksAt50, signalsAt50, deliveryMs, clicksBeforeDispose, signalsBeforeDispose, clicksAfterDispose = clicks, signalsAfterDispose = signals.Count, probe.MouseRegistered, injectedPressesBeforeDispose = 1, sameProcessNeutralTarget = true });
                    }
                    if (Environment.GetEnvironmentVariable("RUNNER_TEMP") is { } diagnostics)
                        File.WriteAllText(Path.Combine(diagnostics, "voxpet-runtime-hand-input-delivery.json"), JsonSerializer.Serialize(deliveries));
                }
                finally { inputTarget.Close(); main.Activate(); }
            }
            using (var reused = new HandMotionInputs(source, _ => { }, () => { }))
            {
                reused.Configure(HandMotionSettings.Default() with { Profiles = HandMotionSettings.Default().Profiles.Select(p => p with { LeftClick = false }).ToArray() });
                check(reused.Registered.Count == 1, "motion_native_key_reusable_after_dispose");
            }
        }
        finally { await hands.CloseAsync(); Directory.Delete(folder, true); }
    }
}
