namespace OpenSubtitleFetcher.Wpf.Helpers.UiContexts;

public static class UiContext
{
    private static IUiContext Instance =>
        _instance ?? throw new NullReferenceException($"{nameof(UiContext)} has to be initialized");

    private static IUiContext? _instance;

    public static void Initialize(IUiContext uiContext)
    {
        _instance ??= uiContext;
    }

    public static Task<T> InvokeAsync<T>(Func<Task<T>> func) => Instance.InvokeAsync(func);
    public static Task InvokeAsync(Func<Task> func) => Instance.InvokeAsync(func);
    public static Task InvokeAsync(Action action) => Instance.InvokeAsync(action);
    public static void Invoke(Action action) => Instance.Invoke(action);
}