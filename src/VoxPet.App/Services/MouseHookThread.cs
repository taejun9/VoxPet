using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace VoxPet.App.Services;

/// <summary>A dedicated message loop keeps native hook delivery independent of UI rendering.</summary>
internal sealed class MouseHookThread : IDisposable
{
    private delegate IntPtr MouseCallback(int code, IntPtr message, IntPtr data);
    private readonly MouseCallback callback;
    private readonly Action<int, long> receive;
    private readonly TaskCompletionSource<bool> ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Dispatcher? dispatcher;
    private IntPtr hook;
    private int disposed;
    public Task<bool> Ready => ready.Task;
    public Task<bool> Stopped => stopped.Task;
    public bool Registered => Volatile.Read(ref disposed) == 0 && Volatile.Read(ref hook) != IntPtr.Zero;
    public int RegistrationError { get; private set; }
    internal int OwnerThreadId { get; private set; }
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int type, MouseCallback procedure, IntPtr module, uint threadId);
    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? name);
    public MouseHookThread(Action<int, long> receive)
    {
        this.receive = receive; callback = Mouse;
        var thread = new Thread(Run) { IsBackground = true, Name = "VoxPet mouse signal" };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
    }
    private void Run()
    {
        try
        {
            OwnerThreadId = Environment.CurrentManagedThreadId;
            var loop = Dispatcher.CurrentDispatcher; Volatile.Write(ref dispatcher, loop);
            if (Volatile.Read(ref disposed) != 0) return;
            hook = SetWindowsHookEx(14, callback, GetModuleHandle(null), 0);
            RegistrationError = hook == IntPtr.Zero ? Marshal.GetLastPInvokeError() : 0;
            if (hook == IntPtr.Zero) return;
            // Report readiness from inside the running owner message loop.
            _ = loop.BeginInvoke(DispatcherPriority.Background, () => ready.TrySetResult(Registered));
            if (Volatile.Read(ref disposed) != 0) loop.BeginInvokeShutdown(DispatcherPriority.Send);
            Dispatcher.Run();
        }
        finally
        {
            var handle = Interlocked.Exchange(ref hook, IntPtr.Zero);
            bool released = handle == IntPtr.Zero || UnhookWindowsHookEx(handle);
            ready.TrySetResult(false); stopped.TrySetResult(released);
        }
    }
    private IntPtr Mouse(int code, IntPtr message, IntPtr data)
    {
        if (Volatile.Read(ref disposed) == 0) receive(code, message.ToInt64());
        return CallNextHookEx(hook, code, message, data);
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        var loop = Volatile.Read(ref dispatcher);
        if (loop != null && !loop.HasShutdownStarted) loop.BeginInvokeShutdown(DispatcherPriority.Send);
    }
}
