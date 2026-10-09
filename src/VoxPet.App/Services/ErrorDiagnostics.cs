using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace VoxPet.App.Services;

/// <summary>마지막 오류의 종류/코드/앱 메서드만 로컬에 기록한다. 메시지·개인 경로·음성은 기록하지 않는다.</summary>
internal static class ErrorDiagnostics
{
    internal static string Folder => AppPaths.DataFolder;
    internal static string Record(Exception exception, string stage, string? folder = null)
    {
        string location = folder ?? Folder;
        string path = Path.Combine(location, "last-error.json");
        try
        {
            var errors = new List<object>();
            for (Exception? error = exception; error != null && errors.Count < 4; error = error.InnerException)
            {
                var methods = new StackTrace(error, false).GetFrames()
                    .Select(frame => frame.GetMethod()).Where(method => method?.DeclaringType?.Namespace?.StartsWith("VoxPet", StringComparison.Ordinal) == true)
                    .Take(12).Select(method => method!.DeclaringType!.FullName + "." + method.Name).ToArray();
                errors.Add(new { type = error.GetType().FullName, hresult = error.HResult.ToString("X8"), methods });
            }
            Directory.CreateDirectory(location);
            File.WriteAllText(path, JsonSerializer.Serialize(new { utc = DateTimeOffset.UtcNow, version = typeof(ErrorDiagnostics).Assembly.GetName().Version?.ToString(), stage, errors }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) { }
        return path;
    }
}
