using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace VoxPet.App.Services;

/// <summary>설정창 HWND에 Ctrl+Shift+F1~F11을 등록하고 해제한다. F12는 로컬 처리하며 키보드 훅/기록은 사용하지 않는다.</summary>
public sealed class ExpressionHotkeys : IDisposable
{
    private const int FirstId = 0x5100;
    private readonly HwndSource source;
    private readonly Action<int> activate;
    private readonly HashSet<int> registered = [];
    private bool disposed;
    /// <summary>전역 등록에 성공한 0기반 슬롯 번호. 실제 키는 F(slot+1)이다.</summary>
    public IReadOnlyCollection<int> Registered => registered;
    public List<int> Conflicts { get; } = [];
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);

    public ExpressionHotkeys(HwndSource source, Action<int> activate)
    {
        this.source = source; this.activate = activate;
        source.AddHook(HandleMessage);
        // F12는 Windows 디버거 예약 키다. 전역 등록하지 않고 두 앱 창의 로컬 입력으로 처리한다.
        for (int slot = 0; slot < 11; slot++)
        {
            if (RegisterHotKey(source.Handle, FirstId + slot, 0x4006, (uint)(0x70 + slot))) registered.Add(slot);
            else Conflicts.Add(slot);
        }
    }
    private IntPtr HandleMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        int slot = (int)wParam.ToInt64() - FirstId;
        long keys = lParam.ToInt64();
        if (!disposed && message == 0x0312 && registered.Contains(slot) &&
            (keys & 0xffff) == 6 && ((keys >> 16) & 0xffff) == 0x70 + slot)
        {
            handled = true; activate(slot);
        }
        return IntPtr.Zero;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (int slot in registered) UnregisterHotKey(source.Handle, FirstId + slot);
        registered.Clear(); source.RemoveHook(HandleMessage);
    }
}
