namespace VoxPet.Core.Models;

/// <summary>위치는 native 화면 좌표, 크기는 WPF DIP. 최대화/최소화 중에도 마지막 일반 크기를 보존한다.</summary>
public sealed record WindowPlacement(double Width, double Height, double? Left = null, double? Top = null, bool Maximized = false)
{
    public WindowPlacement Normalize(bool broadcast = false) => this with
    {
        Width = Size(Width, broadcast ? 200 : 480, broadcast ? 1200 : 16384, broadcast ? 480 : 1000),
        Height = Size(Height, broadcast ? 200 : 320, broadcast ? 1200 : 16384, broadcast ? 480 : 800),
        Left = Coordinate(Left),
        Top = Coordinate(Top),
        Maximized = !broadcast && Maximized
    };
    private static double Size(double value, double min, double max, double fallback) => double.IsFinite(value) && value > 0 ? Math.Clamp(value, min, max) : fallback;
    private static double? Coordinate(double? value) => value is { } number && double.IsFinite(number) && Math.Abs(number) <= 1_000_000 ? number : null;

    /// <summary>겹침이 가장 큰 모니터를 선택하고, 분리된 모니터의 창은 첫 작업 영역으로 이동한다.</summary>
    public WindowPlacement Fit(IReadOnlyList<DesktopArea> areas, double scaleX = 1, double scaleY = 1, bool broadcast = false)
    {
        var placement = Normalize(broadcast);
        var screens = areas.Where(area => area.IsValid).ToArray();
        if (screens.Length == 0) return placement;
        scaleX = double.IsFinite(scaleX) && scaleX > 0 ? scaleX : 1;
        scaleY = double.IsFinite(scaleY) && scaleY > 0 ? scaleY : 1;
        double left = placement.Left ?? double.NaN, top = placement.Top ?? double.NaN;
        var screen = screens.OrderByDescending(area => area.Overlap(left, top, placement.Width * scaleX, placement.Height * scaleY)).First();
        double width = Math.Min(placement.Width, screen.Width / scaleX), height = Math.Min(placement.Height, screen.Height / scaleY);
        bool visible = screen.Overlap(left, top, width * scaleX, height * scaleY) > 0;
        return placement with
        {
            Width = width,
            Height = height,
            Left = visible ? Math.Clamp(left, screen.Left, screen.Left + screen.Width - width * scaleX) : screen.Left + (screen.Width - width * scaleX) / 2,
            Top = visible ? Math.Clamp(top, screen.Top, screen.Top + screen.Height - height * scaleY) : screen.Top + (screen.Height - height * scaleY) / 2
        };
    }
}

public readonly record struct DesktopArea(double Left, double Top, double Width, double Height)
{
    public bool IsValid => double.IsFinite(Left) && double.IsFinite(Top) && double.IsFinite(Width) && double.IsFinite(Height) && Width > 0 && Height > 0;
    public double Overlap(double left, double top, double width, double height) => double.IsFinite(left) && double.IsFinite(top)
        ? Math.Max(0, Math.Min(Left + Width, left + width) - Math.Max(Left, left)) * Math.Max(0, Math.Min(Top + Height, top + height) - Math.Max(Top, top)) : 0;
}
