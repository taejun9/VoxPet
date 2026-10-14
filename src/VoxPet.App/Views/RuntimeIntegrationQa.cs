using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Actions;
using VoxPet.Core.Character;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Packages;
using VoxPet.Core.Triggers;
using CharacterDefinition = VoxPet.Core.Character.CharacterDefinition;

namespace VoxPet.App.Views;

/// <summary>v1 pixel 기준과 synthetic native 패키지의 실제 WPF/저장/입력 수명을 검사한다.</summary>
internal static class RuntimeIntegrationQa
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);
    private static byte[] Render(FrameworkElement view, string? path = null)
    {
        view.Width = view.Height = 512; view.Measure(new Size(512, 512)); view.Arrange(new Rect(0, 0, 512, 512)); view.UpdateLayout();
        var bitmap = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32); bitmap.Render(view);
        if (path != null) { var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var file = File.Create(path); encoder.Save(file); }
        var bytes = new byte[512 * 512 * 4]; bitmap.CopyPixels(bytes, 2048, 0); return bytes;
    }
    public static async Task RunAsync(Action<bool, string> check)
    {
        var character = new CharacterViewModel(); var comparisons = new List<object>();
        void Compare(string name)
        {
            var actual = Render(new MotionSurface { DataContext = character });
            var expected = Render(new LegacyMotionReference { DataContext = character });
            long absolute = 0; int changed = 0;
            for (int i = 0; i < actual.Length; i++) { absolute += Math.Abs(actual[i] - expected[i]); if (actual[i] != expected[i]) changed++; }
            double mean = absolute / (double)actual.Length, fraction = changed / (double)actual.Length;
            comparisons.Add(new { name, mean, fraction });
            check(mean <= 1 && fraction <= .01, "runtime_legacy_pixels_" + name);
        }
        foreach (MouthState mouth in Enum.GetValues<MouthState>()) foreach (int eye in new[] { 0, 1 })
        { character.Update(new((int)mouth / 2d, 0, 0, 0, eye, mouth), 1); Compare("mouth-" + mouth + "-eye-" + eye); }
        var rig = CharacterRig.Default("builtin");
        foreach (HandGesture gesture in Enum.GetValues<HandGesture>()) foreach (double progress in new[] { .1, .5, .9 })
        { character.SetMotion(rig, new(gesture, progress), HandMotionProfile.Default(gesture)); Compare("hand-" + gesture + "-" + progress); }
        character.SetMotion(rig with { HeadMotion = true }, null, null); character.Update(new(.7, 0, 0, 0, 1, MouthState.Open), 2); Compare("head-motion");
        character.Apply(ExpressionProfile.Default(2)); character.Update(new(.5, 0, 0, 0, 1, MouthState.Half), 2.12); Compare("transition-tears");
        character.Update(new(.5, 0, 0, 0, 1, MouthState.Half), 2.5); Compare("settled-tears");
        string evidence = Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath();
        File.WriteAllText(Path.Combine(evidence, "voxpet-runtime-pixel-comparison.json"), JsonSerializer.Serialize(comparisons));
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-runtime-v2-qa-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        var model = new MainViewModel(true, () => [], _ => throw new InvalidOperationException(), dataFolder: folder, charactersFolder: Path.Combine(folder, "missing"), initialSettings: new(new()));
        var window = new MainWindow(true, SystemParameters.WorkArea, model);
        try
        {
            // 기존 public 제작 틀의 원본 PNG로 package codec/worker decode 경계를 실제 실행한다.
            string template = Path.Combine(AppContext.BaseDirectory, "Assets", "ImageSetTemplate", "imageset.json");
            string packagePath = Path.Combine(folder, "fixture.voxpet");
            await Task.Run(() =>
            {
                var original = ImageSetLoader.Load(template).Definition; var definition = new LegacyImageSetSource(original).Load();
                var action = new ActionDefinition("qa-action", "QA 동작", 300, [new(0, ActionEventKind.Transform, "head", transform: new(x: 10))],
                    triggers: [new("qa-key", "qa-action", TriggerKind.Hotkey, "q", modifiers: TriggerModifiers.Ctrl | TriggerModifiers.Alt), new("qa-mouse", "qa-action", TriggerKind.Mouse, button: "left")]);
                definition = new CharacterDefinition(definition.Parts, definition.Anchors, mouth: definition.Mouth, expressions: definition.Expressions,
                    actions: [action], motion: definition.Motion, face: definition.Face);
                var manifest = new VoxPetManifest("qa.native", "Synthetic Native", "1.0", "VoxPet QA", actions: ["actions/qa.json"]);
                var assets = definition.Assets.ToDictionary(path => path, path => File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(template)!, path)));
                new VoxPetPackageExporter().Export(new VoxPetPackageValidator().Validate(manifest, definition, assets), packagePath);
            });
            window.Show(); await window.Ready; var resume = model.PauseVisualForQa();
            check(await model.ImportCharacterAsync(packagePath), "runtime_native_import_worker_decode");
            var scene = model.Packages.Scene!;
            check(model.Packages.IsActive && scene.Images.Values.All(i => i.IsFrozen) && ReferenceEquals(model.Character.RuntimeScene, scene), "runtime_native_frozen_shared_scene");
            double now = scene.Runtime.Actions.LastTime; model.Packages.Update(now + .1, 1);
            check(scene.Runtime.MouthFrame == 2, "runtime_native_voice_mouth");
            model.Packages.Input(new(TriggerKind.Hotkey, "q", modifiers: TriggerModifiers.Ctrl | TriggerModifiers.Alt, isPulse: true)); model.Packages.Update(now + .2, 0);
            check(scene.Runtime.State.ActionId == "qa-action" && scene.Frame.Layers.Single(l => l.Id == "part-head").X == 10, "runtime_native_trigger_action_frame");
            model.Packages.Update(now + .6, 0); check(scene.Runtime.State.ActionId == null, "runtime_native_action_returns_idle");
            Render(new MotionSurface { DataContext = model.Character }, Path.Combine(evidence, "voxpet-runtime-native-frame.png"));
            var broadcast = window.ShowCharacter(); check(ReferenceEquals(broadcast.DataContext, model), "runtime_native_two_windows_share_model");
            var prior = model.Character.RuntimeScene;
            string bad = Path.Combine(folder, "bad.voxpet"); File.WriteAllText(bad, "invalid");
            check(!await model.Packages.ImportAsync(bad) && ReferenceEquals(prior, model.Character.RuntimeScene), "runtime_native_invalid_import_preserves_live_scene");
            var source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle); var pulses = new List<TriggerEvent>();
            using (var inputs = new NativeCharacterInputs(source, pulses.Add))
            {
                var triggers = scene.Runtime.Definition.Actions.SelectMany(a => a.Triggers).ToArray();
                // 모델의 기존 등록을 먼저 해제한다. 같은 키 등록 충돌도 명시적으로 시험한다.
                await model.Packages.DeactivateAsync(); inputs.Configure(triggers, true, false, 1);
                check(inputs.RegisteredKeys == 1 && !inputs.MouseRegistered, "runtime_native_keys_and_mouse_default_off");
                check(NativeCharacterInputs.Binding(new("reserved", "qa-action", TriggerKind.Hotkey, "m", modifiers: TriggerModifiers.Ctrl | TriggerModifiers.Shift)) == null, "runtime_native_reserved_mute_key_preserved");
                PostMessage(source.Handle, 0x0312, new IntPtr(0x6501), IntPtr.Zero); await Task.Delay(30);
                check(pulses.Count == 1 && pulses[0].IsPulse && pulses[0].Modifiers == (TriggerModifiers.Ctrl | TriggerModifiers.Alt), "runtime_native_wm_hotkey_pulse");
                inputs.Configure(triggers, true, true, 2); check(inputs.MouseRegistered, "runtime_native_mouse_explicit_enable");
                mouse_event(2, 0, 0, 0, UIntPtr.Zero); mouse_event(4, 0, 0, 0, UIntPtr.Zero); await Task.Delay(30);
                check(pulses.Any(p => p.Kind == TriggerKind.Mouse && p.Button == "left" && p.IsPulse), "runtime_native_mouse_pulse");
                int count = pulses.Count;
                inputs.Dispose();
                PostMessage(source.Handle, 0x0312, new IntPtr(0x6501), IntPtr.Zero); await Task.Delay(30);
                check(pulses.Count == count, "runtime_native_dispose_ignores_queued_key"); check(inputs.RegisteredKeys == 0 && !inputs.MouseRegistered, "runtime_native_dispose_releases_inputs");
            }
            var library = new CharacterPackageLibrary(Path.Combine(folder, "CharactersV2")); string id = (await Task.Run(library.List)).Single().LibraryId;
            check(await model.Packages.SelectAsync(id), "runtime_native_managed_reselect");
            File.Delete(packagePath); resume(); await window.ShutdownAsync(); window.Close();
            // 기본 legacy 시트가 존재해도 저장한 native 선택을 startup이 지우지 않아야 한다.
            string family = Path.Combine(folder, "legacy-family"); Directory.CreateDirectory(family);
            var sheet = new ImageSource[3, 2];
            var fixtureCharacter = new CharacterViewModel();
            for (int m = 0; m < 3; m++) for (int e = 0; e < 2; e++)
            { fixtureCharacter.Update(new(m / 2d, 0, 0, 0, e, (MouthState)m)); sheet[m, e] = fixtureCharacter.Sprite; }
            File.WriteAllBytes(Path.Combine(family, CharacterCatalog.All.Single(c => c.Id == CharacterCatalog.DefaultId).FileName), CharacterViewModel.EncodeSheet(sheet));
            var restarted = new MainViewModel(true, () => [], _ => throw new InvalidOperationException(), dataFolder: folder, charactersFolder: family, initialSettings: new(new()));
            var restartWindow = new MainWindow(true, SystemParameters.WorkArea, restarted);
            try
            {
                restartWindow.Show(); await restartWindow.Ready;
                check(restarted.Packages.IsActive && restarted.Packages.Loaded!.Package.Manifest.Id == "qa.native", "runtime_native_restart_without_original");
                restarted.DefaultCharacterCommand.Execute(null); check(!restarted.Packages.IsActive, "runtime_native_legacy_restore_keeps_library");
            }
            finally { await restartWindow.ShutdownAsync(); restartWindow.Close(); }
        }
        finally { await window.ShutdownAsync(); window.Close(); Directory.Delete(folder, true); }
    }
}
