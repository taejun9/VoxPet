using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.Services;

public sealed record AudioDevice(string Id, string Name);

/// <summary>Shared-mode WASAPI microphone input. Native errors, including Stop after removal, become state events.</summary>
public sealed class AudioCaptureService : IAudioInput
{
    private readonly MMDevice device;
    private readonly AudioClient client;
    private readonly WaveFormat waveFormat;
    private readonly PcmFormat format;
    private readonly TaskCompletionSource ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Thread? worker;
    private byte[] buffer = [];
    private volatile bool stopRequested;
    private bool disposed;
    public event Action<AudioLevel>? LevelAvailable;
    public event Action<Exception?>? Ended;

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

    public AudioCaptureService(string id)
    {
        using var enumerator = new MMDeviceEnumerator();
        device = enumerator.GetDevice(id);
        try
        {
            client = device.AudioClient;
            try
            {
                // Mirror NAudio's standard conversion of extensible PCM/float; never assume float.
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

    public Task StartAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (worker != null) throw new InvalidOperationException("이미 시작한 마이크 입력입니다.");
        const long duration = 20 * 10000; // 20ms in WASAPI's 100ns units
        client.Initialize(AudioClientShareMode.Shared,
            AudioClientStreamFlags.AutoConvertPcm | AudioClientStreamFlags.SrcDefaultQuality,
            duration, 0, waveFormat, Guid.Empty);
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

    private void ReadAudio()
    {
        var reader = client.AudioCaptureClient;
        // The wait happens only on the capture worker, never Dispatcher.
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
                    // Analyze before Release/next packet; publish only numbers, never PCM references.
                    LevelAvailable?.Invoke(AudioAnalyzer.Analyze(buffer.AsSpan(0, count), format));
                }
                finally { reader.ReleaseBuffer(frames); }
            }
        }
    }

    public async Task StopAsync()
    {
        stopRequested = true;
        if (worker == null) return;
        await ended.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
    }
    public ValueTask DisposeAsync()
    {
        if (disposed) return ValueTask.CompletedTask;
        stopRequested = true;
        worker?.Join(); // AudioSession already awaited completion; still joins before releasing COM.
        Array.Clear(buffer); buffer = [];
        client.Dispose(); device.Dispose(); disposed = true;
        return ValueTask.CompletedTask;
    }
}
