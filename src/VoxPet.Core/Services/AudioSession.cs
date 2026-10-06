using System.Diagnostics;
using VoxPet.Core.Models;

namespace VoxPet.Core.Services;

/// <summary>
/// 플랫폼 입력 계약. LevelAvailable/Ended는 작업 스레드에서 발생할 수 있다.
/// StopAsync는 캡처 종료를 기다려야 하며 완료 후에만 DisposeAsync로 자원을 해제한다.
/// </summary>
public interface IAudioInput : IAsyncDisposable
{
    event Action<AudioLevel>? LevelAvailable;
    event Action<Exception?>? Ended;
    Task StartAsync();
    Task StopAsync();
}

/// <summary>
/// 자원 수명 상태. Faulted에서도 종료 재시도를 위해 입력 자원이 남아 있을 수 있다.
/// </summary>
public enum CaptureState { Stopped, Starting, Running, Stopping, Faulted }
/// <summary>
/// 측정값과 Stopwatch 타임스탬프. 시계 값은 wall-clock 날짜가 아닌 단조 증가 tick이다.
/// </summary>
public sealed record AudioSnapshot(AudioLevel Level, long Timestamp);

/// <summary>
/// SemaphoreSlim으로 Start/Stop/Dispose를 직렬화하고 세션별 측정값을 분리한다.
/// UI와 입력 작업 스레드 사이에는 불변 숫자 snapshot만 Volatile로 전달한다.
/// </summary>
public sealed class AudioSession : IAsyncDisposable
{
    // 콜백 closure는 이 Run만 갱신한다. 이전 장치의 늦은 콜백이 새 Run을 오염시키지 않는다.
    private sealed class Run(IAudioInput input)
    {
        public IAudioInput Input { get; } = input;
        public AudioSnapshot Latest = new(AudioLevel.Silence, 0);
        public Exception? Error;
        public int Ended;
        public Action<AudioLevel>? OnLevel;
        public Action<Exception?>? OnEnd;
    }
    // await로 순서를 기다리므로 UI 스레드를 동기 대기시키지 않는다. 상태값만으로 자원 소유권을 판단하지 않는다.
    private readonly SemaphoreSlim lifecycle = new(1, 1);
    private Run? current;
    private int state;
    private string? error;
    private bool disposed;
    public CaptureState State => (CaptureState)Volatile.Read(ref state);
    public string? Error => Volatile.Read(ref error);
    public bool HasResources => Volatile.Read(ref current) != null;
    public static double StaleSeconds => .25;
    /// <summary>
    /// Running의 최신 측정만 반환한다. 입력 종료 또는 250ms 초과의 오래된 snapshot은 무음으로 취급한다.
    /// 동일한 Stopwatch 시간축의 now를 받으며 이후 Release는 소비자의 Processor가 적용한다.
    /// </summary>
    public AudioLevel ReadLevel(long now)
    {
        var run = Volatile.Read(ref current);
        if (run == null || State != CaptureState.Running || Volatile.Read(ref run.Ended) != 0) return AudioLevel.Silence;
        var snapshot = Volatile.Read(ref run.Latest);
        double age = (now - snapshot.Timestamp) / (double)Stopwatch.Frequency;
        return snapshot.Timestamp == 0 || age < 0 || age > StaleSeconds ? AudioLevel.Silence : snapshot.Level;
    }
    public bool HasInputEnded => Volatile.Read(ref current) is { } run && Volatile.Read(ref run.Ended) != 0;

    /// <summary>
    /// 이전 입력 정리 후 새 입력을 생성·구독·시작한다. 오류는 Faulted와 사용자 안내로 변환한다.
    /// </summary>
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
            // 생성과 초기화는 COM을 호출할 수 있으므로 UI 밖에서 수행한다. worker 콜백은 WPF를 직접 만지지 않는다.
            var input = await Task.Run(factory).ConfigureAwait(false);
            var run = new Run(input);
            run.OnLevel = level => Volatile.Write(ref run.Latest, new(level, Stopwatch.GetTimestamp()));
            // Error를 먼저 기록하고 Ended를 Volatile로 공개해 종료를 읽는 소비자가 원인을 함께 볼 수 있게 한다.
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

    /// <summary>
    /// 정리 도중 도착한 native 종료 오류도 보존한다. 종료 실패 시 자원을 유지해 안전한 재시도를 허용한다.
    /// </summary>
    public async Task StopAsync()
    {
        await lifecycle.WaitAsync().ConfigureAwait(false);
        try
        {
            Volatile.Write(ref state, (int)CaptureState.Stopping);
            var run = Volatile.Read(ref current);
            if (run is { Ended: not 0 }) Volatile.Write(ref error, Describe(run.Error));
            await CleanupAsync().ConfigureAwait(false);
            // Cleanup이 worker 종료를 기다리는 동안 native Stop 오류가 Ended로 도착할 수 있어 완료 후 다시 확인한다.
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

    /// <summary>
    /// 순서: 입력 Stop 완료 → 이벤트 해제 → 입력 Dispose → 현재 Run 해제.
    /// Stop timeout이면 여기서 중단하므로 아직 실행 중인 입력을 강제로 Dispose하지 않는다.
    /// </summary>
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
    // 예외의 개인 경로나 장치 정보를 노출하지 않고 복구 방법이 있는 일반 안내로 바꾼다.
    private static string Describe(Exception? ex) => ex is UnauthorizedAccessException || ex?.HResult == unchecked((int)0x80070005)
        ? "마이크 접근이 거부되었습니다. Windows 설정 → 개인정보 및 보안 → 마이크에서 데스크톱 앱 접근을 허용하세요."
        : ex is NotSupportedException
        ? "지원하지 않는 마이크 샘플 형식입니다. Windows 소리 설정에서 다른 형식이나 장치를 선택하세요."
        : ex is TimeoutException
        ? "마이크 종료가 지연되고 있습니다. 잠시 후 Stop 또는 앱 종료를 다시 시도하세요."
        : "마이크가 중단되었거나 사용할 수 없습니다. 연결 및 Windows 마이크 권한을 확인하고 장치를 새로고침하세요.";
    /// <summary>
    /// 진행 중인 Start/Stop 뒤에서 종료한다. 정리에 실패하면 disposed를 설정하지 않아 재시도할 수 있다.
    /// </summary>
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
