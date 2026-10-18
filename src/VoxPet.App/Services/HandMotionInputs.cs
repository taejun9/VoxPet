using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using VoxPet.Core.Models;
using VoxPet.Core.Triggers;

namespace VoxPet.App.Services;

/// <summary>등록 키와 좌버튼 down 신호만 사용. 좌표/창/입력 내용을 읽거나 저장하지 않으며 원래 클릭을 전달한다.</summary>
public sealed class HandMotionInputs : IDisposable
{
    private const int FirstId = 0x5300;
    private readonly HwndSource source;
    private readonly Action<HandGesture> activate;
    private readonly Action click;
    private readonly MouseCallback callback;
    private readonly Action<int>? trace;
    private readonly Dictionary<int, HandMotionProfile> registered = [];
    private IntPtr mouseHook;
    private bool disposed;
    private int pendingClick;
    private int generation;
    public event Action<TriggerEvent>? Triggered;
    public IReadOnlyCollection<HandGesture> Registered => registered.Values.Select(profile => profile.Gesture).ToArray();
    public bool MouseRegistered => mouseHook != IntPtr.Zero;
    public List<HandGesture> Conflicts { get; } = [];
    private delegate IntPtr MouseCallback(int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int type, MouseCallback procedure, IntPtr module, uint threadId);
    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? name);
    public HandMotionInputs(HwndSource source, Action<HandGesture> activate, Action click) : this(source, activate, click, null) { }
    internal HandMotionInputs(HwndSource source, Action<HandGesture> activate, Action click, Action<int>? trace)
    {
        this.trace = trace;
        this.source = source; this.activate = activate; this.click = click; callback = Mouse;
        source.AddHook(Message);
    }
    public void Configure(HandMotionSettings settings)
    {
        if (disposed) return; settings.Validate();
        generation++;
        foreach (int id in registered.Keys) UnregisterHotKey(source.Handle, id);
        registered.Clear(); Conflicts.Clear();
        foreach (var profile in settings.Profiles.Where(profile => profile.Enabled && profile.Mode == HandTriggerMode.Hotkey && !profile.LeftClick))
        {
            int id = FirstId + (int)profile.Gesture;
            if (RegisterHotKey(source.Handle, id, 0x4000 | profile.Shortcut.Modifiers, profile.Shortcut.Key)) registered.Add(id, profile);
            else Conflicts.Add(profile.Gesture);
        }
        bool wantsMouse = settings.Profiles.Any(profile => profile.Enabled && profile.Mode == HandTriggerMode.Hotkey && profile.LeftClick);
        if (wantsMouse && mouseHook == IntPtr.Zero) mouseHook = SetWindowsHookEx(14, callback, GetModuleHandle(null), 0);
        else if (!wantsMouse && mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(mouseHook); mouseHook = IntPtr.Zero; }
    }
    private IntPtr Mouse(int code, IntPtr message, IntPtr data)
    {
        if (trace != null && code >= 0 && message.ToInt64() is >= 0x0200 and <= 0x020E) trace(1000 + (int)message.ToInt64());
        bool down = code >= 0 && message.ToInt64() == 0x0201;
        if (down) trace?.Invoke(1);
        if (!disposed && mouseHook != IntPtr.Zero && down && Interlocked.Exchange(ref pendingClick, 1) == 0)
        {
            trace?.Invoke(2);
            int revision = generation;
            // hook를 즉시 반환하며 UI 작업은 하나만 예약한다. data 구조를 역참조하지 않는다.
            _ = source.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                Interlocked.Exchange(ref pendingClick, 0);
                if (!disposed && mouseHook != IntPtr.Zero && revision == generation)
                {
                    trace?.Invoke(3);
                    Triggered?.Invoke(new(TriggerKind.Mouse, button: "left", isPulse: true));
                    click();
                }
                else trace?.Invoke(4);
            });
        }
        else if (down) trace?.Invoke(disposed ? 6 : mouseHook == IntPtr.Zero ? 7 : 8);
        return CallNextHookEx(mouseHook, code, message, data);
    }
    private IntPtr Message(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!disposed && message == 0x0312 && registered.TryGetValue((int)wParam.ToInt64(), out var profile))
        {
            long keys = lParam.ToInt64();
            if ((keys & 0xffff) == profile.Shortcut.Modifiers && ((keys >> 16) & 0xffff) == profile.Shortcut.Key)
            {
                handled = true; Triggered?.Invoke(HotkeyTriggerProvider.FromLegacy(profile.Shortcut.Key, profile.Shortcut.Modifiers));
                activate(profile.Gesture);
            }
        }
        return IntPtr.Zero;
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        generation++;
        foreach (int id in registered.Keys) UnregisterHotKey(source.Handle, id);
        registered.Clear(); source.RemoveHook(Message);
        if (mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(mouseHook); mouseHook = IntPtr.Zero; }
    }
}
