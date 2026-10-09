using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;

namespace VoxPet.App.Views;

/// <summary>개인 자료 없이 상세8열의 행열/기본 표정 가족/저장 재시작/손상 복구를 확인한다.</summary>
internal static class DetailedCharacterQa
{
    internal static void SaveFixture(string path, int expression)
    {
        var pixels = new byte[1024 * 256 * 4];
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 8; col++)
                for (int y = 20; y < 116; y++)
                    for (int x = 20; x < 108; x++)
                    {
                        int i = ((row * 128 + y) * 1024 + col * 128 + x) * 4;
                        pixels[i] = (byte)(30 + col * 25); pixels[i + 1] = (byte)(20 + expression * 18);
                        pixels[i + 2] = (byte)(row == 0 ? 70 : 180); pixels[i + 3] = 255;
                    }
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(1024, 256, 96, 96, PixelFormats.Bgra32, null, pixels, 4096)));
        using var file = File.Create(path); encoder.Save(file);
    }
    private static byte[] Sample(ImageSource image)
    {
        var pixels = new byte[4]; ((BitmapSource)image).CopyPixels(new System.Windows.Int32Rect(64, 64, 1, 1), pixels, 4, 0); return pixels;
    }
    public static async Task RunAsync(Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-detailed-qa-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "QA.png");
        string variants = Path.Combine(folder, "QA-표정"); Directory.CreateDirectory(variants);
        ExpressionViewModel? editor = null, restart = null;
        try
        {
            SaveFixture(path, 0);
            foreach (var kind in Enum.GetValues<ExpressionKind>().Where(kind => kind != ExpressionKind.Neutral))
                SaveFixture(Path.Combine(variants, kind.ToString().ToLowerInvariant() + ".png"), (int)kind);
            var character = new CharacterViewModel();
            check(await character.LoadDefaultAsync(path) && character.DefaultExpressionCount == 12 && character.MouthFrameCount == 8, "detailed_default_family_load");
            double[] levels = [0, .05, .15, .30, .45, .65, .82, 1];
            var states = new HashSet<ImageSource>();
            for (int mouth = 0; mouth < levels.Length; mouth++)
                foreach (double eyes in new[] { 0.0, 1.0 })
                {
                    character.Update(new(levels[mouth], 0, 0, 0, eyes, MouthState.Closed));
                    states.Add(character.Sprite); var sample = Sample(character.Sprite);
                    check(sample[0] == 30 + mouth * 25 && sample[2] == (eyes == 1 ? 70 : 180), "detailed_row_column_" + mouth + "_" + eyes);
                }
            check(states.Count == 16 && states.All(image => image.IsFrozen), "detailed_sixteen_frozen_states");
            var familyStates = new HashSet<ImageSource>();
            foreach (var kind in Enum.GetValues<ExpressionKind>())
            {
                character.Apply(ExpressionProfile.Default((int)kind));
                for (int mouth = 0; mouth < levels.Length; mouth++)
                    foreach (double eyes in new[] { 0.0, 1.0 })
                    {
                        character.Update(new(levels[mouth], 0, 0, 0, eyes, MouthState.Closed));
                        familyStates.Add(character.Sprite); var sample = Sample(character.Sprite);
                        check(sample[0] == 30 + mouth * 25 && sample[1] == 20 + (int)kind * 18 && sample[2] == (eyes == 1 ? 70 : 180), $"detailed_default_expression_{kind}_{mouth}_{eyes}");
                    }
            }
            check(familyStates.Count == 192 && familyStates.All(image => image.IsFrozen), "detailed_twelve_families_192_frozen_states");
            var sheet = await Task.Run(() => CharacterSheetLoader.Load(path));
            var encoded = await Task.Run(() => CharacterViewModel.EncodeSheet(sheet));
            var decoded = await Task.Run(() => CharacterSheetLoader.Load(encoded));
            check(decoded.GetLength(0) == 8 && Enumerable.Range(0, 8).All(m => Sample(decoded[m, 1]).SequenceEqual(Sample(sheet[m, 1])) && Sample(decoded[m, 0]).SequenceEqual(Sample(sheet[m, 0]))), "detailed_encode_roundtrip");
            string slots = Path.Combine(folder, "slots");
            editor = new(character, persistent: true, slots); await editor.InitializeAsync();
            check(await editor.ImportAsync(path), "detailed_slot_import"); await editor.SaveAsync();
            restart = new(character, persistent: true, slots); await restart.InitializeAsync();
            character.Update(new(.82, 0, 0, 0, 1, MouthState.Closed));
            check(restart.Slots[0].Saved.SheetId != null && character.MouthFrameCount == 8 && Sample(character.Sprite)[0] == 180, "detailed_slot_survives_restart");
            File.WriteAllText(Path.Combine(variants, "sad.png"), "bad PNG");
            check(await character.LoadDefaultAsync(path) && character.DefaultWarnings == 1 && character.DefaultExpressionCount == 11, "detailed_corrupt_variant_recovers");
            character.Apply(ExpressionProfile.Default(2)); character.Update(new(0, 0, 0, 0, 1, MouthState.Closed));
            check(Sample(character.Sprite)[1] == 20, "detailed_corrupt_variant_uses_neutral");
            File.Delete(path);
            var before = character.Sprite;
            check(!await character.LoadDefaultAsync(path) && ReferenceEquals(before, character.Sprite), "detailed_missing_default_preserves_current");
        }
        finally
        {
            if (editor != null) await editor.CloseAsync(); if (restart != null) await restart.CloseAsync();
            Directory.Delete(folder, recursive: true);
        }
    }
}
