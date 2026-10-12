using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows.Interop;

namespace VoxPet.App.Services;

/// <summary>사용자/Windows 세션별 mutex. 두 번째 실행은 UI 생성 전에 기존 HWND에 복원 요청만 보낸다.</summary>
internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex mutex;
    private readonly uint message;
    private bool owned;
    private HwndSource? source;
    private Action? activate;
    private delegate bool WindowCallback(IntPtr window, IntPtr state);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string name);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool EnumWindows(WindowCallback callback, IntPtr state);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder title, int capacity);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool AllowSetForegroundWindow(uint process);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool PostMessage(IntPtr window, uint message, IntPtr parameter, IntPtr data);
    internal SingleInstance()
    {
        string user = WindowsIdentity.GetCurrent().User!.Value;
        mutex = new(false, "Local\\VoxPet-single-" + user); message = RegisterWindowMessage("VoxPet-activate-" + user);
        try { owned = mutex.WaitOne(0); } catch (AbandonedMutexException) { owned = true; }
    }
    internal bool IsFirst => owned;
    internal void RequestExisting()
    {
        EnumWindows((window, _) =>
        {
            var title = new StringBuilder(256); GetWindowText(window, title, title.Capacity);
            if (title.ToString() != "VoxPet · 목소리로 움직이는 캐릭터") return true;
            GetWindowThreadProcessId(window, out uint process); AllowSetForegroundWindow(process);
            PostMessage(window, message, IntPtr.Zero, IntPtr.Zero); return true;
        }, IntPtr.Zero);
    }
    internal void Attach(HwndSource source, Action activate) { this.source = source; this.activate = activate; source.AddHook(OnMessage); }
    private IntPtr OnMessage(IntPtr window, int code, IntPtr first, IntPtr second, ref bool handled)
    {
        if ((uint)code == message) { handled = true; activate?.Invoke(); }
        return IntPtr.Zero;
    }
    public void Dispose() { source?.RemoveHook(OnMessage); if (owned) { owned = false; mutex.ReleaseMutex(); } mutex.Dispose(); }
}
