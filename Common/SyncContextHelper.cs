namespace SecureChat.Common;

public static class SyncContextHelper
{
    private static SynchronizationContext? _uiContext;

    public static void Initialize()
    {
        _uiContext = SynchronizationContext.Current
            ?? new WindowsFormsSynchronizationContext();
    }

    public static void Post(Action action)
    {
        if (_uiContext != null)
            _uiContext.Post(_ => action(), null);
        else
            action();
    }

    public static void Send(Action action)
    {
        if (_uiContext != null)
            _uiContext.Send(_ => action(), null);
        else
            action();
    }
}
