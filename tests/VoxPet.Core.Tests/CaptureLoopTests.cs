using VoxPet.Core.Services;
using Xunit;

namespace VoxPet.Core.Tests;

/// <summary>native 캡처/Stop의 실패 조합에서 최초 원인 보존과 완료 호출 순서를 확인한다.</summary>
public sealed class CaptureLoopTests
{
    [Fact]
    public void RemovedDeviceAndFailingStopStillReportOriginalError()
    {
        var removal = new IOException("Device removed"); Exception? result = null; bool stopped = false, completed = false;
        CaptureLoop.Run(() => throw removal, () => { stopped = true; throw new IOException("Stop also failed"); }, ex => { completed = true; result = ex; });
        Assert.True(stopped); Assert.True(completed); Assert.Same(removal, result);
    }
    [Fact]
    public void StopFailureBecomesRecoverableCompletionError()
    {
        var stop = new IOException("Native stop failed"); Exception? result = null;
        CaptureLoop.Run(() => { }, () => throw stop, ex => result = ex);
        Assert.Same(stop, result);
    }
    [Fact]
    public void NormalCompletionReportsOnceAfterNativeStop()
    {
        var order = new List<string>();
        CaptureLoop.Run(() => order.Add("capture"), () => order.Add("stop"), ex => { Assert.Null(ex); order.Add("completion"); });
        Assert.Equal(new[] { "capture", "stop", "completion" }, order);
    }
}
