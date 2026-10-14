namespace VoxPet.Core.Character;

/// <summary>정의 생성 경계의 공통 검증. 파일을 읽거나 경로를 추출하지 않는다.</summary>
internal static class DefinitionValidation
{
    public static string Id(string value, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or '.')))
            throw new ArgumentException("ID는 영문·숫자·-·_·.을 사용해야 합니다.", parameter);
        return value;
    }

    public static string Text(string value, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl))
            throw new ArgumentException("비어 있지 않은 텍스트가 필요합니다.", parameter);
        return value;
    }

    public static double Finite(double value, string parameter)
    {
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(parameter, "유한한 수치가 필요합니다.");
        return value;
    }

    public static double Positive(double value, string parameter)
    {
        Finite(value, parameter);
        if (value <= 0) throw new ArgumentOutOfRangeException(parameter, "양수가 필요합니다.");
        return value;
    }

    public static double Unit(double value, string parameter)
    {
        Finite(value, parameter);
        if (value is < 0 or > 1) throw new ArgumentOutOfRangeException(parameter, "0~1 범위가 필요합니다.");
        return value;
    }

    public static string Asset(string value, string extension, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains('\\') || value.Contains(':') || value.Any(char.IsControl) ||
            value.Split('/').Any(segment => segment is "" or "." or "..") || !value.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("파일 참조는 패키지 내부의 상대 경로여야 합니다.", parameter);
        return value;
    }

    public static IReadOnlyList<T> Copy<T>(IEnumerable<T> values, string parameter) where T : class
    {
        ArgumentNullException.ThrowIfNull(values, parameter);
        T[] copy = values.ToArray();
        if (copy.Any(value => value is null)) throw new ArgumentException("null 항목은 허용되지 않습니다.", parameter);
        return Array.AsReadOnly(copy);
    }
}
