namespace VoxPet.Core.Services;

/// <summary>장치 제거 후 native Stop도 실패하더라도 worker 종료 결과를 보고하는 공통 실행 틀.</summary>
public static class CaptureLoop
{
    /// <summary>
    /// 캡처의 최초 오류를 보존한다. 캡처가 정상이었으면 Stop 오류를 종료 원인으로 전달한다.
    /// completed는 native Stop 시도 후 한 번 호출되어 세션의 종료 대기를 풀 수 있다.
    /// </summary>
    public static void Run(Action capture, Action stopNative, Action<Exception?> completed)
    {
        Exception? error = null;
        try { capture(); }
        catch (Exception ex) { error = ex; }
        finally
        {
            try { stopNative(); }
            catch (Exception ex) { error ??= ex; }
            completed(error);
        }
    }
}
