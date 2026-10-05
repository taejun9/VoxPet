using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.Services;
using VoxPet.Core.Models;

namespace VoxPet.App.Views;

internal static class CharacterQa
{
    private static string QaPath(string name) => Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), name);
    public static async Task RunAsync(MainWindow main, Action<bool, string> check)
    {
        main.Model.StopCommand.Execute(null); await Task.Delay(100);
        main.Model.Character.Update(new(0, 0, 0, 0, 1, MouthState.Closed));
        string sheetPath = QaPath("voxpet-character-fixture.png");
        var pixels = new byte[384 * 256 * 4];
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 3; col++)
                for (int y = row * 128 + 20 - row * 4; y < row * 128 + 115 - row * 4; y++)
                    for (int x = col * 128 + 20 + col * 4; x < col * 128 + 108 + col * 4; x++)
                    {
                        int i = (y * 384 + x) * 4;
                        pixels[i] = (byte)(40 + col * 70); pixels[i + 1] = (byte)(70 + row * 110); pixels[i + 2] = 200; pixels[i + 3] = 255;
                    }
        var fixture = BitmapSource.Create(384, 256, 96, 96, PixelFormats.Bgra32, null, pixels, 384 * 4);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(fixture));
        using (var file = File.Create(sheetPath)) encoder.Save(file);
        var initial = main.Model.Character.Sprite;
        check(await main.Model.ImportCharacterAsync(sheetPath), "character_valid_sheet_import");
        var images = new HashSet<ImageSource>();
        foreach (var mouth in Enum.GetValues<MouthState>())
            foreach (double eyes in new[] { 0.0, 1.0 })
            {
                main.Model.Character.Update(new(0, 0, 0, 0, eyes, mouth));
                images.Add(main.Model.Character.Sprite);
                var bitmap = (BitmapSource)main.Model.Character.Sprite;
                var sample = new byte[4]; bitmap.CopyPixels(new Int32Rect(64, 64, 1, 1), sample, 4, 0);
                var full = new byte[128 * 128 * 4]; bitmap.CopyPixels(full, 128 * 4, 0);
                check(full[(27 * 128 + 20) * 4 + 3] == 255 && full[(121 * 128 + 107) * 4 + 3] == 255 &&
                    full[(26 * 128 + 20) * 4 + 3] == 0 && full[(122 * 128 + 107) * 4 + 3] == 0, "character_shifted_cells_align");
                check(sample[0] == 40 + (int)mouth * 70 && sample[1] == (eyes == 1 ? 70 : 180), "character_sheet_row_column_mapping");
            }
        check(images.Count == 6 && images.All(image => image.IsFrozen && image.Width == 128), "character_six_frozen_states");
        string invalid = QaPath("voxpet-character-invalid.png"); File.WriteAllText(invalid, "invalid PNG");
        check(!await main.Model.ImportCharacterAsync(invalid) && images.Contains(main.Model.Character.Sprite), "character_failed_import_preserves_previous");
        check(!await main.Model.ImportCharacterAsync(invalid + ".missing"), "character_missing_file_recovers");
        var oversized = new byte[33];
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(oversized, 0);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(oversized.AsSpan(8, 4), 13);
        "IHDR"u8.CopyTo(oversized.AsSpan(12));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(oversized.AsSpan(16, 4), 30000);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(oversized.AsSpan(20, 4), 20000);
        oversized[24] = 8; oversized[25] = 6; File.WriteAllBytes(invalid, oversized);
        check(!await main.Model.ImportCharacterAsync(invalid), "character_oversized_header_rejected_before_decode");
        SaveImage(BitmapSource.Create(384, 256, 96, 96, PixelFormats.Bgra32, null, new byte[pixels.Length], 384 * 4), "voxpet-character-invalid.png");
        check(!await main.Model.ImportCharacterAsync(invalid), "character_empty_sheet_rejected");
        for (int i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
        SaveImage(BitmapSource.Create(384, 256, 96, 96, PixelFormats.Bgra32, null, pixels, 384 * 4), "voxpet-character-invalid.png");
        check(!await main.Model.ImportCharacterAsync(invalid), "character_opaque_sheet_rejected");
        using (var file = File.Create(invalid)) file.SetLength(16 * 1024 * 1024 + 1);
        check(!await main.Model.ImportCharacterAsync(invalid), "character_oversized_file_rejected");
        File.Delete(sheetPath); check(!File.Exists(sheetPath), "character_sheet_file_released");
        main.Model.DefaultCharacterCommand.Execute(null);
        main.Model.Character.Update(new(0, 0, 0, 0, 1, MouthState.Closed));
        check(ReferenceEquals(initial, main.Model.Character.Sprite), "character_restore_default");
        main.Model.SoftVoiceCommand.Execute(null);
        check(main.Model.NoiseGate == -65 && main.Model.NormalizeMin == -65 && main.Model.NormalizeMax == -20, "preset_soft_voice");
        main.Model.SnappyCommand.Execute(null);
        check(main.Model.AttackMs == 15 && main.Model.ReleaseMs == 80, "preset_snappy");
        main.Model.ConversationCommand.Execute(null);
        check(main.Model.NoiseGate == -50 && main.Model.Sensitivity == 1, "preset_conversation");
        main.Model.DemoCommand.Execute(null);
        main.Model.CharacterMuted = true;
        bool rawContinues = false, blinkContinues = false;
        var end = DateTime.UtcNow.AddSeconds(6.5);
        while (DateTime.UtcNow < end)
        {
            await Task.Delay(20); rawContinues |= main.Model.RawLevel > .2;
            blinkContinues |= main.Model.Character.Sprite is BitmapImage bmp && bmp.UriSource.ToString().Contains("-blink");
            check(main.Model.VoiceLevel == 0, "mute_closes_mouth");
        }
        check(rawContinues && blinkContinues, "mute_keeps_input_and_blink");
        main.Model.CharacterMuted = false;
        bool reacted = false;
        for (int i = 0; i < 150; i++) { await Task.Delay(20); reacted |= main.Model.VoiceLevel > .2; }
        check(reacted, "unmute_resumes_reaction");
        main.Model.StopCommand.Execute(null); await Task.Delay(100);
        check(main.Model.VoiceLevel == 0 && main.Model.RawLevel == 0, "stop_after_unmute_resets");
        // Optional PRIVATE test sheet injected through a manual dispatch; never part of the public default assets.
        string? personal = Environment.GetEnvironmentVariable("VOXPET_QA_SHEET");
        if (!string.IsNullOrWhiteSpace(personal))
        {
            bool loaded = await main.Model.ImportCharacterAsync(personal); check(loaded, "personal_sheet_import");
            if (loaded)
            {
                foreach (var mouth in Enum.GetValues<MouthState>())
                    foreach (double eyes in new[] { 0.0, 1.0 })
                    {
                        main.Model.Character.Update(new(0, 0, 0, 0, eyes, mouth));
                        SaveImage((BitmapSource)main.Model.Character.Sprite, $"voxpet-personal-{mouth}-{eyes:F0}.png");
                    }
                main.Model.DemoCommand.Execute(null);
                var states = new HashSet<ImageSource>();
                for (int i = 0; i < 400; i++) { await Task.Delay(20); states.Add(main.Model.Character.Sprite); }
                check(states.Count >= 4, "personal_sheet_synthetic_animation");
                main.Width = 1000; main.Height = 730; await Task.Delay(100); main.UpdateLayout();
                var screen = new RenderTargetBitmap(1000, 730, 96, 96, PixelFormats.Pbgra32); screen.Render(main);
                SaveImage(screen, "voxpet-personal-preview.png");
                main.Model.StopCommand.Execute(null); await Task.Delay(100);
            }
        }
        File.Delete(invalid);
    }
    private static void SaveImage(BitmapSource bitmap, string name)
    {
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(QaPath(name)); encoder.Save(file);
    }
}
