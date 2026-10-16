using System.IO;
using System.Text.Json;
using VoxPet.App.Services;
using VoxPet.App.ViewModels;
using VoxPet.Core.Packages;

namespace VoxPet.App.Views;

/// <summary>실제 worker PNG/카드의 문자열 변환을 UI 스레드에서 검사한다.</summary>
internal static class CardDisplayQa
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        var loaded = await Task.Run(() => CharacterPackageLibrary.Decode(new VoxPetPackageLoader().Load(StudioShellViewModel.SamplePath)));
        var image = loaded.Images[loaded.Package.Manifest.Thumbnail!];
        check(image.IsFrozen, "card_display_worker_thumbnail_frozen");
        var card = new CharacterCard("qa-library", "QA 카드", "qa.card", "1.0", "QA", "QA", image);
        bool safe = false;
        try { safe = card.ToString() == card.Name; }
        catch (InvalidOperationException error)
        {
            string folder = Path.Combine(Path.GetTempPath(), "VoxPet-card-repro-" + Guid.NewGuid());
            try
            {
                string diagnostic = ErrorDiagnostics.Record(error, "card-display-repro", folder);
                string output = Path.Combine(Environment.GetEnvironmentVariable("RUNNER_TEMP") ?? Path.GetTempPath(), "voxpet-runtime-card-display-repro.json");
                File.Copy(diagnostic, output, true);
                using var document = JsonDocument.Parse(File.ReadAllText(diagnostic));
                check(document.RootElement.GetProperty("errors")[0].GetProperty("methods").EnumerateArray().Any(m => m.GetString() == "VoxPet.App.ViewModels.CharacterCard.PrintMembers"), "card_display_original_printmembers_failure_reproduced");
            }
            finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        }
        check(safe, "card_display_worker_thumbnail_to_name_without_exception");
    }
}
