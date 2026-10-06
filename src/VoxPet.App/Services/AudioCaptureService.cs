using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.Services;

/// <summary>
/// 장치 목록의 표시용 정보. Id는 현재 실행에서만 사용하며 영구 설정에 저장하지 않는다.
/// </summary>
public sealed record AudioDevice(string Id, string Name);

/// <summary>
/// 선택한 마이크 endpoint의 shared-mode WASAPI 입력. system loopback을 사용하지 않는다.
/// 전용 worker가 native 버퍼를 읽고 숫자만 발행하며, 장치 제거와 Stop 오류는 Ended로 보고한다.
/// </summary>
public sealed class AudioCaptureService : IAudioInput
{
    private readonly MMDevice device;
    private readonly AudioClient client;
    private readonly WaveFormat waveFormat;
    private readonly PcmFormat format;
    // 종료 continuation이 캡처 스레드 안에서 실행되지 않도록 비동기로 예약한다.
    private readonly TaskCompletionSource ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Thread? worker;
    private byte[] buffer = [];
    private volatile bool stopRequested;
    private bool disposed;
    public event Action<AudioLevel>? LevelAvailable;
    public event Action<Exception?>? Ended;

    /// <summary>
    /// 현재 활성 입력 endpoint만 열거하고 COM 객체를 즉시 해제해 표시 정보만 반환한다.
    /// </summary>
    public static IReadOnlyList<AudioDevice> ListDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        var collection = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        var result = new List<AudioDevice>();
        foreach (var endpoint in collection)
        {
            using (endpoint) result.Add(new(endpoint.ID, endpoint.FriendlyName));
        }
        return result;
    }

    /// <summary>
    /// AudioSession이 UI 밖에서 생성한다. 부분 초기화 실패 시 이미 얻은 client/device를 역순으로 해제한다.
    /// </summary>
    public AudioCaptureService(string id)
    {
        using var enumerator = new MMDeviceEnumerator();
        device = enumerator.GetDevice(id);
        try
        {
            client = device.AudioClient;
            try
            {
                // 확장 WaveFormat을 NAudio의 표준 형식으로 해석한다. 실제 PCM/float 비트 수와 채널을 검증한다.
                waveFormat = client.MixFormat;
                var standard = waveFormat.AsStandardWaveFormat();
                format = new(standard.Encoding switch
                {
                    WaveFormatEncoding.Pcm => SampleEncoding.Pcm,
                    WaveFormatEncoding.IeeeFloat => SampleEncoding.Float,
                    _ => throw new NotSupportedException("지원하지 않는 마이크 형식입니다.")
                }, standard.BitsPerSample, standard.Channels);
                format.Validate();
            }
            catch { client.Dispose(); throw; }
        }
        catch { device.Dispose(); throw; }
    }

    /// <summary>
    /// shared-mode 버퍼를 초기화하고 전용 스레드를 시작한다. 입력 인스턴스는 한 번만 시작할 수 있다.
    /// </summary>
    public Task StartAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (worker != null) throw new InvalidOperationException("이미 시작한 마이크 입력입니다.");
        const long duration = 20 * 10000; // WASAPI의 100ns 단위로 환산한 20ms 버퍼
        client.Initialize(AudioClientShareMode.Shared,
            AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality,
            duration, 0, waveFormat, Guid.Empty);
        // 프레임 수×BlockAlign을 checked로 계산하고 PCM 작업 메모리를 16MiB 이하로 제한한다.
        int capacity = checked(client.BufferSize * waveFormat.BlockAlign);
        if (capacity is <= 0 or > 16 * 1024 * 1024) throw new NotSupportedException("마이크 버퍼 크기가 지원 범위를 벗어납니다.");
        buffer = new byte[capacity];
        worker = new Thread(() => CaptureLoop.Run(ReadAudio, client.Stop, ex =>
        {
            try { Ended?.Invoke(ex); }
            finally { ended.TrySetResult(); }
        }))
        { IsBackground = true, Name = "VoxPet microphone" };
        try { worker.Start(); }
        catch { worker = null; throw; }
        return Task.CompletedTask;
    }

    /// <summary>
    /// native 버퍼의 소유권은 GetBuffer~ReleaseBuffer 구간에만 유효하다.
    /// 예외가 발생해도 finally에서 반환하며 재사용 PCM 배열 참조를 외부에 넘기지 않는다.
    /// </summary>
    private void ReadAudio()
    {
        var reader = client.AudioCaptureClient;
        // 버퍼 주기의 절반마다 확인한다. Sleep은 캡처 worker에서만 수행하며 Dispatcher를 막지 않는다.
        int waitMs = (int)Math.Clamp(client.BufferSize * 1000L / waveFormat.SampleRate / 2, 1, 100);
        client.Start();
        while (!stopRequested)
        {
            Thread.Sleep(waitMs);
            while (!stopRequested && reader.GetNextPacketSize() > 0)
            {
                IntPtr native = reader.GetBuffer(out int frames, out AudioClientBufferFlags flags);
                try
                {
                    int count = checked(frames * waveFormat.BlockAlign);
                    if (count < 0 || count > buffer.Length) throw new InvalidOperationException("잘못된 마이크 버퍼입니다.");
                    if ((flags & AudioClientBufferFlags.Silent) != 0) Array.Clear(buffer, 0, count);
                    else Marshal.Copy(native, buffer, 0, count);
                    // 다음 패킷이 덮어쓰기 전에 즉시 분석한다. 콜백에는 PCM 참조 대신 불변 측정값만 전달한다.
                    LevelAvailable?.Invoke(AudioAnalyzer.Analyze(buffer.AsSpan(0, count), format));
                }
                finally { reader.ReleaseBuffer(frames); }
            }
        }
    }

    /// <summary>
    /// 종료를 요청하고 최대 5초 동안 worker 완료를 기다린다. timeout은 세션이 처리하며 COM을 강제 해제하지 않는다.
    /// </summary>
    public async Task StopAsync()
    {
        stopRequested = true;
        if (worker == null) return;
        await ended.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
    }
    /// <summary>
    /// AudioSession이 Stop 완료 후 UI 밖에서 호출한다. worker와 합류한 뒤 PCM 메모리를 지우고 COM을 해제한다.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        if (disposed) return ValueTask.CompletedTask;
        stopRequested = true;
        worker?.Join(); // Stop 완료를 기다린 뒤에도 COM 해제 전에 worker 종료를 확실히 확인한다.
        Array.Clear(buffer); buffer = [];
        client.Dispose(); device.Dispose(); disposed = true;
        return ValueTask.CompletedTask;
    }
}
