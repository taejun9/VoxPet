using NAudio.CoreAudioApi;
using NAudio.Wave;
using VoxPet.Core.Models;
using VoxPet.Core.Services;

namespace VoxPet.App.Services;

public sealed record AudioDevice(string Id, string Name);

/// <summary>WASAPI microphone adapter. Constructed off Dispatcher to avoid NAudio capturing a UI context.</summary>
public sealed class AudioCaptureService : IAudioInput
{
    private readonly MMDevice device;
    private readonly WasapiCapture capture;
    private readonly PcmFormat format;
    private readonly TaskCompletionSource ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool started;
    private bool disposed;
    private Exception? analysisError;
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
            capture = new WasapiCapture(device, false, 20);
            try
            {
                var wave = capture.WaveFormat; // NAudio exposes extensible format as its standard PCM/float equivalent.
                format = new(wave.Encoding switch
                {
                    WaveFormatEncoding.Pcm => SampleEncoding.Pcm,
                    WaveFormatEncoding.IeeeFloat => SampleEncoding.Float,
                    _ => throw new NotSupportedException("지원하지 않는 마이크 형식입니다.")
                }, wave.BitsPerSample, wave.Channels);
                format.Validate();
                capture.DataAvailable += OnData;
                capture.RecordingStopped += OnStopped;
            }
            catch { capture.Dispose(); throw; }
        }
        catch { device.Dispose(); throw; }
    }
    private void OnData(object? sender, WaveInEventArgs e)
    {
        try { LevelAvailable?.Invoke(AudioAnalyzer.Analyze(e.Buffer.AsSpan(0, e.BytesRecorded), format)); }
        catch (Exception ex)
        {
            analysisError = ex;
            capture.StopRecording();
        }
    }
    private void OnStopped(object? sender, StoppedEventArgs e)
    {
        ended.TrySetResult();
        Ended?.Invoke(analysisError ?? e.Exception);
    }
    public Task StartAsync()
    {
        capture.StartRecording();
        started = true;
        return Task.CompletedTask;
    }
    public async Task StopAsync()
    {
        if (!started) return;
        capture.StopRecording();
        await ended.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
    }
    public ValueTask DisposeAsync()
    {
        if (disposed) return ValueTask.CompletedTask;
        capture.DataAvailable -= OnData;
        capture.RecordingStopped -= OnStopped;
        capture.Dispose(); // joins the native capture thread before releasing the audio client
        device.Dispose();
        disposed = true;
        return ValueTask.CompletedTask;
    }
}
