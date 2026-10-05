using System.Diagnostics;
using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

public interface IAudioInput : IAsyncDisposable
{
    event Action<AudioLevel>? LevelAvailable;
    event Action<Exception?>? Ended;
    Task StartAsync();
    Task StopAsync();
}

public enum CaptureState { Stopped, Starting, Running, Stopping, Faulted }
public sealed record AudioSnapshot(AudioLevel Level, long Timestamp);

/// <summary>Serializes lifecycle work and isolates each session's callbacks and numeric snapshots.</summary>
public sealed class AudioSession : IAsyncDisposable
{
    private sealed class Run(IAudioInput input)
    {
        public IAudioInput Input { get; } = input;
        public AudioSnapshot Latest = new(AudioLevel.Silence, 0);
        public Exception? Error;
        public int Ended;
        public Action<AudioLevel>? OnLevel;
        public Action<Exception?>? OnEnd;
    }
    private readonly SemaphoreSlim lifecycle = new(1, 1);
    private Run? current;
    private int state;
    private string? error;
    private bool disposed;
    public CaptureState State => (CaptureState)Volatile.Read(ref state);
    public string? Error => Volatile.Read(ref error);
    public bool HasResources => Volatile.Read(ref current) != null;
    public static double StaleSeconds => .25;
    public AudioLevel ReadLevel(long now)
    {
        var run = Volatile.Read(ref current);
        if (run == null || State != CaptureState.Running || Volatile.Read(ref run.Ended) != 0) return AudioLevel.Silence;
        var snapshot = Volatile.Read(ref run.Latest);
        double age = (now - snapshot.Timestamp) / (double)Stopwatch.Frequency;
        return snapshot.Timestamp == 0 || age < 0 || age > StaleSeconds ? AudioLevel.Silence : snapshot.Level;
    }
    public bool HasInputEnded => Volatile.Read(ref current) is { } run && Volatile.Read(ref run.Ended) != 0;

    public async Task StartAsync(Func<IAudioInput> factory)
    {
        await lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (State is CaptureState.Running or CaptureState.Starting) return;
            await CleanupAsync().ConfigureAwait(false);
            Volatile.Write(ref error, null);
            Volatile.Write(ref state, (int)CaptureState.Starting);
            // Construction and capture initialization may call COM; keep them off the UI thread.
            var input = await Task.Run(factory).ConfigureAwait(false);
            var run = new Run(input);
            run.OnLevel = level => Volatile.Write(ref run.Latest, new(level, Stopwatch.GetTimestamp()));
            run.OnEnd = ex => { run.Error = ex; Volatile.Write(ref run.Ended, 1); };
            input.LevelAvailable += run.OnLevel;
            input.Ended += run.OnEnd;
            Volatile.Write(ref current, run);
            await Task.Run(input.StartAsync).ConfigureAwait(false);
            Volatile.Write(ref state, (int)CaptureState.Running);
        }
        catch (ObjectDisposedException) when (disposed) { throw; }
        catch (Exception ex)
        {
            Volatile.Write(ref error, Describe(ex));
            try { await CleanupAsync().ConfigureAwait(false); }
            catch { Volatile.Write(ref error, "마이크 종료 처리에 실패했습니다. 앱을 종료하세요."); }
            Volatile.Write(ref state, (int)CaptureState.Faulted);
        }
        finally { lifecycle.Release(); }
    }

    public async Task StopAsync()
    {
        await lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            Volatile.Write(ref state, (int)CaptureState.Stopping);
            var run = Volatile.Read(ref current);
            if (run is { Ended: not 0 }) Volatile.Write(ref error, Describe(run.Error));
            await CleanupAsync().ConfigureAwait(false);
            // A native Stop error can arrive through Ended while cleanup awaits the worker.
            if (run?.Error is { } endedError) Volatile.Write(ref error, Describe(endedError));
            Volatile.Write(ref state, Error == null ? (int)CaptureState.Stopped : (int)CaptureState.Faulted);
        }
        catch (Exception ex)
        {
            Volatile.Write(ref error, Describe(ex));
            Volatile.Write(ref state, (int)CaptureState.Faulted);
        }
        finally { lifecycle.Release(); }
    }

    private async Task CleanupAsync()
    {
        var run = Volatile.Read(ref current);
        if (run == null) return;
        await Task.Run(run.Input.StopAsync).ConfigureAwait(false);
        run.Input.LevelAvailable -= run.OnLevel;
        run.Input.Ended -= run.OnEnd;
        await Task.Run(async () => await run.Input.DisposeAsync().ConfigureAwait(false)).ConfigureAwait(false);
        Volatile.Write(ref current, null);
    }
    private static string Describe(Exception? ex) => ex is UnauthorizedAccessException || ex?.HResult == unchecked((int)0x80070005)
        ? "마이크 접근이 거부되었습니다. Windows 설정 → 개인정보 및 보안 → 마이크에서 데스크톱 앱 접근을 허용하세요."
        : ex is NotSupportedException
        ? "지원하지 않는 마이크 샘플 형식입니다. Windows 소리 설정에서 다른 형식이나 장치를 선택하세요."
        : ex is TimeoutException
        ? "마이크 종료가 지연되고 있습니다. 잠시 후 Stop 또는 앱 종료를 다시 시도하세요."
        : "마이크가 중단되었거나 사용할 수 없습니다. 연결 및 Windows 마이크 권한을 확인하고 장치를 새로고침하세요.";
    public async ValueTask DisposeAsync()
    {
        await lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            if (disposed) return;
            await CleanupAsync().ConfigureAwait(false);
            disposed = true;
            Volatile.Write(ref state, (int)CaptureState.Stopped);
        }
        finally { lifecycle.Release(); }
    }
}
