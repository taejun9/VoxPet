using System.IO;

namespace VoxPet.App.Services;

internal static class AppPaths
{
    internal static string? QaFolder
    {
        get
        {
            string? requested = Environment.GetEnvironmentVariable("VOXPET_QA_DATA_DIR");
            string? temporary = Environment.GetEnvironmentVariable("RUNNER_TEMP");
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true" || string.IsNullOrWhiteSpace(requested) || string.IsNullOrWhiteSpace(temporary)) return null;
            string path = Path.GetFullPath(requested), root = Path.GetFullPath(temporary).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? path : null;
        }
    }
    internal static string DataFolder => QaFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VoxPet");
}
