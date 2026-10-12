using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using VoxPet.Core.Models;

namespace VoxPet.App.Services;

/// <summary>native 화면 좌표를 같은 API 경계에서 읽고 복원한다. WPF DIP 크기와 분리해 DPI 좌표 혼용을 피한다.</summary>
internal static class WindowPlacementService
{
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsZoomed(IntPtr hwnd);

    internal static WindowPlacement Capture(Window window, WindowPlacement? previous = null)
    {
        var handle = new WindowInteropHelper(window).Handle;
        bool minimized = window.WindowState == WindowState.Minimized || IsIconic(handle);
        bool maximized = window.WindowState == WindowState.Maximized || IsZoomed(handle);
        if (minimized || maximized)
            return (previous ?? new(window.Width, window.Height)) with { Maximized = maximized || (minimized && previous?.Maximized == true) };
        return GetWindowRect(handle, out var rect) ? new(window.Width, window.Height, rect.Left, rect.Top) : previous ?? new(window.Width, window.Height);
    }
    internal static void Restore(Window window, WindowPlacement placement, bool broadcast = false)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (!GetWindowRect(handle, out var rect)) return;
        double scaleX = (rect.Right - rect.Left) / window.Width, scaleY = (rect.Bottom - rect.Top) / window.Height;
        var areas = System.Windows.Forms.Screen.AllScreens.OrderByDescending(screen => screen.Primary)
            .Select(screen => new DesktopArea(screen.WorkingArea.Left, screen.WorkingArea.Top, screen.WorkingArea.Width, screen.WorkingArea.Height)).ToArray();
        var fitted = placement.Fit(areas, scaleX, scaleY, broadcast);
        window.SetCurrentValue(Window.WidthProperty, fitted.Width); window.SetCurrentValue(Window.HeightProperty, fitted.Height);
        // SWP_NOSIZE|NOZORDER|NOACTIVATE: DIP 크기는 WPF가 관리하고 위치만 native 좌표로 이동한다.
        SetWindowPos(handle, IntPtr.Zero, (int)Math.Round(fitted.Left ?? rect.Left), (int)Math.Round(fitted.Top ?? rect.Top), 0, 0, 0x0015);
    }
}
