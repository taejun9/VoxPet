using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace VoxPet.App.Services;

/// <summary>Ctrl+Shift+M으로 입 반응만 전환한다. 키 훅/기록 없이 HWND 수명에 맞춰 등록·해제한다.</summary>
public sealed class MuteHotkey : IDisposable
{
    private const int Id = 0x5200;
    private readonly HwndSource source;
    private readonly Action toggle;
    private bool disposed;
    public bool Registered { get; private set; }
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);
    public MuteHotkey(HwndSource source, Action toggle)
    {
        this.source = source; this.toggle = toggle;
        Registered = RegisterHotKey(source.Handle, Id, 0x4006, 0x4d);
        source.AddHook(HandleMessage);
    }
    private IntPtr HandleMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!disposed && Registered && message == 0x0312 && wParam.ToInt64() == Id &&
            (lParam.ToInt64() & 0xffff) == 6 && ((lParam.ToInt64() >> 16) & 0xffff) == 0x4d)
        { handled = true; toggle(); }
        return IntPtr.Zero;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (Registered) UnregisterHotKey(source.Handle, Id);
        Registered = false; source.RemoveHook(HandleMessage);
    }
}
