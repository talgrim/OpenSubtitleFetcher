namespace OpenSubtitleFetcher.Wpf.Helpers.UiContexts;

public interface IUiContext
{
    Task<T> InvokeAsync<T>(Func<Task<T>> func);
    Task InvokeAsync(Func<Task> func);
    Task InvokeAsync(Action action);
    void Invoke(Action action);
}