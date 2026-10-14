using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Threading;
using VoxPet.Core.Legacy;
using VoxPet.Core.Models;
using VoxPet.Core.Triggers;

namespace VoxPet.App.Services;

/// <summary>native 정의의 선택된 단축키/명시 활성화된 좌클릭만 pulse로 전달한다.</summary>
public sealed class NativeCharacterInputs : IDisposable
{
    private readonly HwndSource source;
    private readonly Action<TriggerEvent> input;
    private readonly Dictionary<int, TriggerEvent> registered = [];
    private delegate IntPtr MouseCallback(int code, IntPtr message, IntPtr data);
    private readonly MouseCallback mouseCallback;
    private IntPtr mouse;
    private bool disposed;
    private int generation;
    private int nextId = 0x6500;
    private long revision = -1;
    private readonly HashSet<string> buttons = [];
    private bool mouseSetting, active;
    public string Status { get; private set; } = "";
    public int RegisteredKeys => registered.Count;
    public bool MouseRegistered => mouse != IntPtr.Zero;
    [DllImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] private static extern bool RegisterHotKey(IntPtr window, int id, uint flags, uint key);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnregisterHotKey(IntPtr window, int id);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int type, MouseCallback callback, IntPtr module, uint threadId);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);
    public NativeCharacterInputs(HwndSource source, Action<TriggerEvent> input)
    { this.source = source; this.input = input; mouseCallback = Mouse; source.AddHook(Message); }
    internal static RuntimeBinding? Binding(TriggerDefinition trigger)
    {
        if (!trigger.Enabled || trigger.Kind != TriggerKind.Hotkey || trigger.Key is null) return null;
        uint key = trigger.Key switch
        {
            "space" => 0x20,
            { Length: 1 } value when char.IsAsciiLetterOrDigit(value[0]) => char.ToUpperInvariant(value[0]),
            var value when value.StartsWith('f') && int.TryParse(value.AsSpan(1), out int f) && f is >= 1 and <= 11 => (uint)(0x6f + f),
            _ => 0
        };
        uint flags = (uint)((trigger.Modifiers.HasFlag(TriggerModifiers.Alt) ? 1 : 0) | (trigger.Modifiers.HasFlag(TriggerModifiers.Ctrl) ? 2 : 0) | (trigger.Modifiers.HasFlag(TriggerModifiers.Shift) ? 4 : 0));
        var binding = new RuntimeBinding(RuntimeOperation.Play, key, flags);
        try { binding.Validate(); return binding; } catch (ArgumentException) { return null; }
    }
    public void Configure(IEnumerable<TriggerDefinition> definitions, bool enabled, bool mouseEnabled, long sceneRevision)
    {
        if (disposed || revision == sceneRevision && active == enabled && mouseSetting == mouseEnabled) return;
        Clear(); revision = sceneRevision; active = enabled; mouseSetting = mouseEnabled;
        if (!enabled) { Status = ""; return; }
        var configured = definitions.ToArray();
        int failures = 0;
        foreach (var group in configured.Where(t => t.Enabled && t.Kind == TriggerKind.Hotkey).GroupBy(t => (t.Key, t.Modifiers)))
        {
            var definition = group.First(); var binding = Binding(definition);
            // scene 변경 때 이전 WM_HOTKEY가 새 scene에 전달되지 않도록 HWND 수명 중 ID를 재사용하지 않는다.
            if (registered.Count >= 128 || binding == null || nextId >= 0xbfff) { failures++; continue; }
            int id = ++nextId;
            if (!RegisterHotKey(source.Handle, id, binding.Modifiers | 0x4000, binding.Key)) { failures++; continue; }
            registered.Add(id, new(TriggerKind.Hotkey, definition.Key, modifiers: definition.Modifiers, isPulse: true));
        }
        if (mouseEnabled) foreach (var trigger in configured.Where(t => t.Enabled && t.Kind == TriggerKind.Mouse)) buttons.Add(trigger.Button!);
        if (buttons.Count > 0)
        { mouse = SetWindowsHookEx(14, mouseCallback, GetModuleHandle(null), 0); if (mouse == IntPtr.Zero) failures++; }
        Status = failures == 0 ? "캐릭터 전역 입력 준비 · 좌클릭은 별도로 활성화하세요." : $"{failures}개 입력 미등록 · 예약/충돌 키는 수동 재생을 사용하세요.";
    }
    private IntPtr Message(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (!disposed && message == 0x0312 && registered.TryGetValue(wParam.ToInt32(), out var signal)) { input(signal); handled = true; }
        return IntPtr.Zero;
    }
    private IntPtr Mouse(int code, IntPtr message, IntPtr data)
    {
        string? button = message.ToInt64() switch { 0x0201 => "left", 0x0204 => "right", 0x0207 => "middle", _ => null };
        if (!disposed && code >= 0 && button != null && buttons.Contains(button))
        {
            int token = generation;
            _ = source.Dispatcher.BeginInvoke(DispatcherPriority.Input, () => { if (!disposed && token == generation) input(new(TriggerKind.Mouse, button: button, isPulse: true)); });
        }
        return CallNextHookEx(mouse, code, message, data);
    }
    private void Clear()
    {
        generation++; buttons.Clear(); foreach (int id in registered.Keys) UnregisterHotKey(source.Handle, id); registered.Clear();
        if (mouse != IntPtr.Zero) { UnhookWindowsHookEx(mouse); mouse = IntPtr.Zero; }
    }
    public void Dispose() { if (disposed) return; disposed = true; Clear(); source.RemoveHook(Message); }
}
