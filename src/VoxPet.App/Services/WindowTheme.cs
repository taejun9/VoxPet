using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VoxPet.App.Services;

/// <summary>기본 창 이동·크기 조절·캡션 버튼을 유지하며 지원 OS의 제목 표시줄 색상을 맞춘다.</summary>
internal static class WindowTheme
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    internal static void Apply(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        int dark = 1, caption = 0x00221707, text = 0x00F9F5F1, border = 0x005E4624;
        // 미지원 속성의 HRESULT는 기본 OS 캡션을 유지한다. 창 동작에는 영향을 주지 않는다.
        _ = DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
        _ = DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int));
        _ = DwmSetWindowAttribute(handle, 36, ref text, sizeof(int));
        _ = DwmSetWindowAttribute(handle, 34, ref border, sizeof(int));
    }
}
