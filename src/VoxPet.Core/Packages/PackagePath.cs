using System.Text;

namespace VoxPet.Core.Packages;

public static class PackagePath
{
    private static readonly HashSet<string> devices = new(new[] { "CON", "PRN", "AUX", "NUL" }
        .Concat(Enumerable.Range(1, 9).SelectMany(i => new[] { "COM" + i, "LPT" + i }))
        .Concat(new[] { "COM¹", "COM²", "COM³", "LPT¹", "LPT²", "LPT³" }), StringComparer.OrdinalIgnoreCase);
    public static string Normalize(string path, bool directory = false)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > 240 || path.Any(c => char.IsControl(c) || c is '\\' or ':' or '<' or '>' or '"' or '|' or '?' or '*')) throw new VoxPetPackageException("패키지 경로가 잘못되었습니다.");
        if (directory && path.EndsWith('/')) path = path[..^1];
        path = path.Normalize(NormalizationForm.FormC);
        foreach (string segment in path.Split('/'))
            if (segment is "" or "." or ".." || segment.EndsWith('.') || segment.EndsWith(' ') || segment.StartsWith(' ') || devices.Contains(segment.Split('.')[0]))
                throw new VoxPetPackageException("패키지 내부 상대 경로만 허용합니다.");
        if (!directory && !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new VoxPetPackageException("PNG/JSON 파일만 지원합니다.");
        return path;
    }
}
