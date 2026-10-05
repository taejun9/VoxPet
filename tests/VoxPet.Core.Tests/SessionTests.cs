using System.Diagnostics;
using VoxPet.Core.Models;
using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

public sealed class SessionTests
{
    private sealed class FakeInput : IAudioInput
    {
        public event Action<AudioLevel>? LevelAvailable;
        public event Action<Exception?>? Ended;
        public int Starts, Stops, Disposals;
        public Exception? StartError;
        public bool FailStop;
        public Exception? EndDuringStopError;
        public TaskCompletionSource? StartBarrier;
        public Action<AudioLevel>? SavedCallback;
        public async Task StartAsync()
        {
            Starts++; SavedCallback = LevelAvailable;
            if (StartBarrier != null) await StartBarrier.Task;
            if (StartError != null) throw StartError;
        }
        public Task StopAsync()
        {
            Stops++;
            if (FailStop) throw new IOException("Fake stop failure");
            Ended?.Invoke(EndDuringStopError); return Task.CompletedTask;
        }
        public ValueTask DisposeAsync() { Disposals++; Assert.True(Stops > 0); return ValueTask.CompletedTask; }
        public void Push() => LevelAvailable?.Invoke(new(1, 1, 0));
        public void Fail() => Ended?.Invoke(new IOException("Fake device unplugged"));
    }
    [Fact]
    public async Task StartStopRepeatAndOldCallbacksAreIsolated()
    {
        await using var session = new AudioSession(); var first = new FakeInput();
        await session.StartAsync(() => first); first.Push();
        Assert.Equal(1, session.ReadLevel(Stopwatch.GetTimestamp()).Rms);
        await session.StartAsync(() => throw new Exception("Must not create a second input"));
        Assert.Equal(1, first.Starts);
        await session.StopAsync(); await session.StopAsync();
        Assert.Equal(AudioLevel.Silence, session.ReadLevel(Stopwatch.GetTimestamp()));
        Assert.Equal(1, first.Disposals);
        var second = new FakeInput(); await session.StartAsync(() => second);
        first.SavedCallback!(new(1, 1, 0)); Assert.Equal(AudioLevel.Silence, session.ReadLevel(Stopwatch.GetTimestamp()));
        second.Push(); Assert.Equal(1, session.ReadLevel(Stopwatch.GetTimestamp()).Rms);
        await session.StopAsync(); Assert.Equal(1, second.Disposals);
    }
    [Fact]
    public async Task StaleInputReleasesAfter250Milliseconds()
    {
        await using var session = new AudioSession(); var input = new FakeInput(); await session.StartAsync(() => input);
        input.Push(); var now = Stopwatch.GetTimestamp();
        Assert.Equal(1, session.ReadLevel(now).Rms);
        Assert.Equal(AudioLevel.Silence, session.ReadLevel(now + Stopwatch.Frequency));
    }
    [Fact]
    public async Task StartFailureDisposesAndAllowsRestart()
    {
        await using var session = new AudioSession(); var input = new FakeInput { StartError = new UnauthorizedAccessException() };
        await session.StartAsync(() => input);
        Assert.Equal(CaptureState.Faulted, session.State); Assert.Contains("거부", session.Error);
        Assert.Equal(1, input.Disposals);
        var next = new FakeInput(); await session.StartAsync(() => next);
        Assert.Equal(CaptureState.Running, session.State); Assert.Null(session.Error);
    }
    [Fact]
    public async Task FactoryFailureAndUnexpectedEndAreRecoverable()
    {
        await using var session = new AudioSession(); await session.StartAsync(() => throw new IOException());
        Assert.Equal(CaptureState.Faulted, session.State);
        var input = new FakeInput(); await session.StartAsync(() => input); input.Push(); input.Fail();
        Assert.True(session.HasInputEnded); Assert.Equal(AudioLevel.Silence, session.ReadLevel(Stopwatch.GetTimestamp()));
        await session.StopAsync(); Assert.Equal(CaptureState.Faulted, session.State); Assert.NotNull(session.Error); Assert.Equal(1, input.Disposals);
    }
    [Fact]
    public async Task ConcurrentStopWaitsForStartingSession()
    {
        await using var session = new AudioSession();
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var input = new FakeInput { StartBarrier = barrier };
        var start = session.StartAsync(() => input); var stop = session.StopAsync();
        barrier.SetResult(); await Task.WhenAll(start, stop);
        Assert.Equal(CaptureState.Stopped, session.State); Assert.Equal(1, input.Disposals);
    }
    [Fact]
    public async Task DisposeQueuesBehindStartAndIsIdempotent()
    {
        var session = new AudioSession(); var input = new FakeInput();
        var start = session.StartAsync(() => input); var dispose = session.DisposeAsync().AsTask();
        await Task.WhenAll(start, dispose); await session.DisposeAsync();
        Assert.Equal(1, input.Disposals); Assert.Equal(AudioLevel.Silence, session.ReadLevel(Stopwatch.GetTimestamp()));
    }
    [Fact]
    public async Task StopFailureRetainsInputForSafeRetry()
    {
        await using var session = new AudioSession(); var input = new FakeInput { FailStop = true };
        await session.StartAsync(() => input); await session.StopAsync();
        Assert.Equal(CaptureState.Faulted, session.State); Assert.Equal(0, input.Disposals); Assert.True(session.HasResources);
        input.FailStop = false; await session.StopAsync(); Assert.Equal(1, input.Disposals); Assert.False(session.HasResources);
    }
    [Fact]
    public async Task NativeEndErrorDuringStopIsReportedAfterCleanupAndAllowsRestart()
    {
        await using var session = new AudioSession();
        var input = new FakeInput { EndDuringStopError = new IOException("Native Stop failed") };
        await session.StartAsync(() => input); input.Push();
        await session.StopAsync();
        Assert.Equal(CaptureState.Faulted, session.State); Assert.NotNull(session.Error);
        Assert.False(session.HasResources); Assert.Equal(1, input.Disposals);
        Assert.Equal(AudioLevel.Silence, session.ReadLevel(Stopwatch.GetTimestamp()));
        await session.StartAsync(() => new FakeInput());
        Assert.Equal(CaptureState.Running, session.State); Assert.Null(session.Error);
    }
    [Fact]
    public async Task StartingDisposedSessionCannotAcquireMicrophone()
    {
        var session = new AudioSession(); await session.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => session.StartAsync(() => throw new Exception("Must not open")));
        Assert.Equal(CaptureState.Stopped, session.State); Assert.False(session.HasResources);
    }
    [Fact]
    public async Task RepeatedConcurrentRequestsDoNotLeak()
    {
        await using var session = new AudioSession(); var inputs = new List<FakeInput>();
        for (int i = 0; i < 100; i++)
        {
            await session.StartAsync(() => { var f = new FakeInput(); inputs.Add(f); return f; });
            await Task.WhenAll(session.StopAsync(), session.StopAsync());
        }
        Assert.Equal(100, inputs.Count); Assert.All(inputs, input => Assert.Equal(1, input.Disposals));
    }
}
