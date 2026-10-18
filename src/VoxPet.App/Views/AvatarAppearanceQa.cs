using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;
using VoxPet.Core.Motion;

namespace VoxPet.App.Views;

internal static class AvatarAppearanceQa
{
    internal static async Task<double> RunAsync(MainWindow window, MainViewModel model, string id, Action<bool, string> check)
    {
        var character = model.Character; var scene = character.CompanionScene;
        await scene.FacePreparation.WaitAsync(TimeSpan.FromSeconds(10));
        check(scene.FacePrepared && scene.FaceError == null, "avatar_face_worker_prepared_" + id);
        double now = scene.Runtime.Actions.LastTime;
        void Tick(double dt, double voice = 0, bool blink = false)
        {
            now = Math.Max(now, scene.Runtime.Actions.LastTime) + dt; model.Hands.Update(now);
            character.Update(new(voice, 0, 0, 0, blink ? 0 : 1, voice < .2 ? MouthState.Closed : voice < .6 ? MouthState.Half : MouthState.Open), now);
        }
        string Face() => scene.Frame.Layers.Single(l => l.Id == "part-head").Asset;
        Tick(.3); string open = Face();
        var mouths = new List<object>(); var seen = new HashSet<string>();
        int positions = (character.MouthFrameCount - 1) * (id == "pelly" ? 1 : 2) + 1;
        for (int i = 0; i < positions; i++)
        {
            double level = i / (positions - 1d); Tick(.016, level); string asset = Face(); seen.Add(asset);
            check(scene.Images[asset].IsFrozen, $"avatar_face_frozen_{id}_{i}"); mouths.Add(new { level, asset });
        }
        check(seen.Count == positions, "avatar_lips_have_intermediate_frames_" + id);
        Tick(.1); var eyes = new HashSet<string> { Face() };
        Tick(0, blink: true);
        for (int i = 0; i < 5; i++) { Tick(.016, blink: true); eyes.Add(Face()); }
        check(eyes.Count >= 4 && character.IsBlinking, "avatar_blink_has_closing_frames_" + id);
        string closed = Face(); Tick(0);
        var opening = new HashSet<string> { closed };
        for (int i = 0; i < 8; i++) { Tick(.016); opening.Add(Face()); }
        check(opening.Count >= 4 && Face() == open, "avatar_blink_opens_smoothly_to_original_" + id);
        var pixel = new byte[4]; ((BitmapSource)scene.Images[closed]).CopyPixels(new(id == "pelly" ? 300 : 256, 160, 1, 1), pixel, 4, 0);
        check(pixel[3] == 255 && scene.Frame.Layers.Where(l => l.Id.StartsWith("part-", StringComparison.Ordinal)).All(l => l.Opacity == 1), "avatar_face_and_body_stay_opaque_" + id);
        var editable = await character.EditableCompanionAsync();
        check(editable?.Character.Motion.Acting == (id == "pelly" ? CompanionActing.Pelly : CompanionActing.Neulbo), "avatar_edit_export_keeps_acting_profile_" + id);
        window.ShowStudio(3);
        await ScreenEvidence.CaptureAsync(window, "avatar-" + id + "-idle", check);
        Tick(.2, .8); await ScreenEvidence.CaptureAsync(window, "avatar-" + id + "-voice", check);
        Tick(.05, .5, true); Tick(.032, .5, true); await ScreenEvidence.CaptureAsync(window, "avatar-" + id + "-blink", check);
        character.PlayCompanionAction("one-hand"); Tick(0); Tick(.5); var before = scene.Runtime.CurrentRig.Animated["rightHand"];
        character.StopCompanionActions(); Tick(0); var stopped = scene.Runtime.CurrentRig.Animated["rightHand"];
        check(Math.Abs(before.OffsetX - stopped.OffsetX) < .00001 && Math.Abs(before.OffsetY - stopped.OffsetY) < .00001, "avatar_ui_stop_preserves_current_pose_" + id);
        await ScreenEvidence.CaptureAsync(window, "avatar-" + id + "-transition", check); Tick(.3);

        // Measure actual composition callbacks while the normal visible surface animates.
        // No screenshots or RenderTargetBitmap calls occur inside this interval.
        var watch = Stopwatch.StartNew(); var intervals = new List<double>(); double previous = -1, started = now;
        EventHandler render = (_, _) =>
        {
            double elapsed = watch.Elapsed.TotalSeconds;
            if (previous >= 0) intervals.Add((elapsed - previous) * 1000); previous = elapsed;
            Tick(Math.Max(0, started + elapsed - now), .45 + .25 * Math.Sin(elapsed * 8), elapsed is > .6 and < .74);
        };
        CompositionTarget.Rendering += render;
        try { await Task.Delay(1500); } finally { CompositionTarget.Rendering -= render; }
        check(intervals.Count >= 5 && scene.FaceError == null, "avatar_visible_composition_remains_live_" + id);
        intervals.Sort();
        if (Environment.GetEnvironmentVariable("RUNNER_TEMP") is { } folder)
            File.WriteAllText(Path.Combine(folder, "voxpet-runtime-avatar-appearance-" + id + ".json"), JsonSerializer.Serialize(new
            {
                mouths,
                closing = eyes,
                opening,
                facePrepared = scene.FacePrepared,
                sourcePixelsNotUploaded = true,
                compositionFrames = intervals.Count,
                compositionMedianMs = intervals.Count == 0 ? 0 : intervals[intervals.Count / 2],
                compositionP95Ms = intervals.Count == 0 ? 0 : intervals[(int)((intervals.Count - 1) * .95)],
                rendererTier = RenderCapability.Tier >> 16,
                measuredOnSyntheticRunner = true
            }));
        Tick(.3); return now;
    }
}
