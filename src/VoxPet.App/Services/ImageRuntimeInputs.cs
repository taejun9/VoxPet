using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using VoxPet.Core.Models;

namespace VoxPet.App.Services;

/// <summary>바인딩된 가상 키와 좌클릭 down만 감지하고 원래 입력은 전달한다. 입력 문자열·좌표를 기록하지 않는다.</summary>
public sealed class ImageRuntimeInputs : IDisposable
{
    private delegate IntPtr InputCallback(int code, IntPtr message, IntPtr data);
    private readonly HwndSource source;
    private readonly Action<RuntimeOperation> execute;
    private readonly InputCallback keyboard, mouse;
    private readonly HashSet<uint> pressed = [];
    private RuntimeBinding[] bindings = [];
    private IntPtr keyboardHook, mouseHook;
    private bool disposed;
    private int generation;
    private long scene;
    public bool KeyboardRegistered => keyboardHook != IntPtr.Zero;
    public bool MouseRegistered => mouseHook != IntPtr.Zero;
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int type, InputCallback callback, IntPtr module, uint threadId);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
    public ImageRuntimeInputs(HwndSource source, Action<RuntimeOperation> execute)
    { this.source = source; this.execute = execute; keyboard = Keyboard; mouse = Mouse; }
    public void Configure(RuntimeBinding[] keys, bool leftClick, bool enabled, long sceneRevision = 0)
    {
        if (disposed) return;
        RuntimeBinding[] wanted = enabled ? keys : [];
        bool wantsMouse = enabled && leftClick;
        if (scene == sceneRevision && bindings.SequenceEqual(wanted) && KeyboardRegistered == (wanted.Length > 0) && MouseRegistered == wantsMouse) return;
        Clear(); scene = sceneRevision; bindings = wanted;
        foreach (uint key in bindings.Select(b => b.Key).Distinct())
            if (GetAsyncKeyState((int)key) < 0) pressed.Add(key);
        if (bindings.Length > 0) keyboardHook = SetWindowsHookEx(13, keyboard, GetModuleHandle(null), 0);
        if (wantsMouse) mouseHook = SetWindowsHookEx(14, mouse, GetModuleHandle(null), 0);
    }
    private void Queue(RuntimeOperation operation)
    {
        int revision = generation;
        _ = source.Dispatcher.BeginInvoke(DispatcherPriority.Input, () => { if (!disposed && revision == generation) execute(operation); });
    }
    private IntPtr Keyboard(int code, IntPtr message, IntPtr data)
    {
        if (!disposed && keyboardHook != IntPtr.Zero && code >= 0)
        {
            uint key = (uint)Marshal.ReadInt32(data);
            // 바인딩에 없는 키는 보관하지 않는다. 반복 down은 한 번만 실행한다.
            if (bindings.Any(b => b.Key == key))
            {
                long type = message.ToInt64();
                if (type is 0x0101 or 0x0105) pressed.Remove(key);
                else if (type is 0x0100 or 0x0104 && pressed.Add(key))
                {
                    uint flags = (uint)((GetAsyncKeyState(0x12) < 0 ? 1 : 0) | (GetAsyncKeyState(0x11) < 0 ? 2 : 0) | (GetAsyncKeyState(0x10) < 0 ? 4 : 0) | (GetAsyncKeyState(0x5b) < 0 || GetAsyncKeyState(0x5c) < 0 ? 8 : 0));
                    var binding = bindings.FirstOrDefault(b => b.Key == key && b.Modifiers == flags);
                    if (binding != null) Queue(binding.Operation);
                }
            }
        }
        return CallNextHookEx(keyboardHook, code, message, data);
    }
    private IntPtr Mouse(int code, IntPtr message, IntPtr data)
    {
        if (!disposed && mouseHook != IntPtr.Zero && code >= 0 && message.ToInt64() == 0x0201) Queue(RuntimeOperation.Play);
        return CallNextHookEx(mouseHook, code, message, data);
    }
    private void Clear()
    {
        generation++; pressed.Clear();
        if (keyboardHook != IntPtr.Zero) { UnhookWindowsHookEx(keyboardHook); keyboardHook = IntPtr.Zero; }
        if (mouseHook != IntPtr.Zero) { UnhookWindowsHookEx(mouseHook); mouseHook = IntPtr.Zero; }
    }
    public void Dispose() { if (disposed) return; disposed = true; Clear(); bindings = []; }
}
