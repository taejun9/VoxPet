namespace VoxPet.Core.Services;

/// <summary>Always reports worker completion even when a removed device throws again during Stop.</summary>
public static class CaptureLoop
{
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
