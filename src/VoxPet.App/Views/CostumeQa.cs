using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VoxPet.App.ViewModels;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.Views;

internal static class CostumeQa
{
    private static void Fixture(string path)
    {
        byte[] pixels = new byte[64 * 64 * 4];
        for (int i = 0; i < pixels.Length; i += 4) { pixels[i] = 20; pixels[i + 1] = 30; pixels[i + 2] = 245; pixels[i + 3] = 255; }
        var bitmap = BitmapSource.Create(64, 64, 96, 96, PixelFormats.Bgra32, null, pixels, 256);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path); encoder.Save(file);
    }
    private static byte[] Render(CharacterViewModel character, string name)
    {
        var view = new CharacterView { DataContext = character, Width = 512, Height = 512 };
        view.Measure(new Size(512, 512)); view.Arrange(new Rect(0, 0, 512, 512)); view.UpdateLayout();
        var bitmap = new RenderTargetBitmap(512, 512, 96, 96, PixelFormats.Pbgra32); bitmap.Render(view);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var file = File.Create(Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), name))) encoder.Save(file);
        var pixels = new byte[512 * 512 * 4]; bitmap.CopyPixels(pixels, 512 * 4, 0); return pixels;
    }
    internal static async Task RunAsync(MainWindow main, Action<bool, string> check)
    {
        string folder = Path.Combine(Path.GetTempPath(), "VoxPet-costume-qa-" + Guid.NewGuid()); Directory.CreateDirectory(folder);
        string original = Path.Combine(folder, "fixture.png"), library = Path.Combine(folder, "managed"); Fixture(original);
        byte[] before = File.ReadAllBytes(original);
        var wardrobe = new CostumeViewModel(true, library);
        try
        {
            foreach (var category in Enum.GetValues<CostumeCategory>())
            {
                wardrobe.ImportCategory = category;
                check(await wardrobe.ImportAsync(original), "costume_import_" + category);
            }
            var one = wardrobe.Items.Single(item => item.Category == CostumeCategory.OnePiece);
            var top = wardrobe.Items.Single(item => item.Category == CostumeCategory.Top);
            var bottom = wardrobe.Items.Single(item => item.Category == CostumeCategory.Bottom);
            var outer = wardrobe.Items.Single(item => item.Category == CostumeCategory.Outerwear);
            check(one.Enabled && !top.Enabled && !bottom.Enabled && outer.Enabled, "costume_onepiece_excludes_separates_keeps_outerwear");
            await wardrobe.WearAsync(top, true); await wardrobe.WearAsync(bottom, true);
            check(!one.Enabled && top.Enabled && bottom.Enabled && outer.Enabled, "costume_separates_exclude_onepiece");
            foreach (var category in new[] { CostumeCategory.FaceDecoration, CostumeCategory.Earrings })
            {
                wardrobe.ImportCategory = category; await wardrobe.ImportAsync(original);
                check(wardrobe.Items.Count(item => item.Category == category && item.Enabled) == 2, "costume_multiple_" + category);
            }
            wardrobe.RemoveAllCommand.Execute(null); await wardrobe.WearAsync(top, true);
            top.X = 12.25; top.Y = 21.5; top.SizePercent = 50;
            wardrobe.RightCommand.Execute(null); wardrobe.DownCommand.Execute(null);
            check(top.X == 13.25 && top.Y == 22.5 && top.DisplayWidth == 115 && top.DisplayHeight == 115, "costume_fine_position_uniform_size");
            wardrobe.Selected = null; check(ReferenceEquals(wardrobe.Selected, top), "costume_transient_null_selection_retained");
            wardrobe.UpLayerCommand.Execute(null); var order = wardrobe.Items.Select(item => item.Id).ToArray();
            await wardrobe.SaveAsync(); check(File.ReadAllBytes(original).SequenceEqual(before), "costume_original_unchanged");
            var priorLayers = main.Model.Character.CostumeLayers;
            main.Model.Character.CostumeLayers = wardrobe.Items;
            try
            {
                var preview = Render(main.Model.Character, "voxpet-costume-preview.png");
                var broadcast = main.ShowCharacter();
                var shared = ((MainViewModel)broadcast.DataContext).Character;
                var live = Render(shared, "voxpet-costume-broadcast.png");
                int sample = (50 * 512 + 50) * 4;
                check(preview.SequenceEqual(live) && preview[sample + 2] > 230 && preview[sample + 1] < 50 && ReferenceEquals(shared.CostumeLayers, wardrobe.Items), "costume_preview_broadcast_share_position_pixels");
            }
            finally { main.Model.Character.CostumeLayers = priorLayers; }
            File.Delete(original);
            var restart = new CostumeViewModel(true, library); await restart.InitializeAsync();
            var restored = restart.Items.Single(item => item.Id == top.Id);
            check(restart.Items.Select(item => item.Id).SequenceEqual(order) && restored.X == 13.25 && restored.Y == 22.5 && restored.SizePercent == 50 && restored.Enabled && restored.Image is BitmapSource { IsFrozen: true }, "costume_restart_after_original_removed");
            await restart.CloseAsync();
            var store = new CostumeStore(library); var saved = store.Load().Items;
            string index = File.ReadAllText(Path.Combine(library, "wardrobe.json"));
            string bad = Path.Combine(folder, "bad.png"); File.WriteAllText(bad, new string('x', 64));
            check(!await wardrobe.ImportAsync(bad) && File.ReadAllText(Path.Combine(library, "wardrobe.json")) == index && wardrobe.Items.Count == 10, "costume_bad_import_preserves_library");
            File.WriteAllText(Path.Combine(library, top.Id.ToString("N") + ".img"), "broken");
            var recover = new CostumeViewModel(true, library); await recover.InitializeAsync();
            check(!recover.Items.Single(item => item.Id == top.Id).Enabled && recover.Items.Count == 10 && store.Load().Items.SequenceEqual(saved), "costume_bad_managed_image_recovers_without_rewrite");
            await recover.CloseAsync();
            wardrobe.Selected = top; wardrobe.DeleteCommand.Execute(null);
            for (int i = 0; i < 100 && !wardrobe.CanEdit; i++) await Task.Delay(20);
            check(!wardrobe.Items.Contains(top) && !File.Exists(Path.Combine(library, top.Id.ToString("N") + ".img")), "costume_delete_owned_copy");
        }
        finally { await wardrobe.CloseAsync(); Directory.Delete(folder, true); }
    }
}
