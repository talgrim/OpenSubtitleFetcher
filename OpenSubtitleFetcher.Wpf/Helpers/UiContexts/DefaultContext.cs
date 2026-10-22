using System.Windows.Threading;

namespace OpenSubtitleFetcher.Wpf.Helpers.UiContexts;

public sealed class DefaultContext : IUiContext
{
    private readonly Dispatcher _dispatcher;

    public bool IsSynchronized => _dispatcher.Thread == Thread.CurrentThread;

    public DefaultContext()
    {
        _dispatcher = System.Windows.Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    public async Task<T> InvokeAsync<T>(Func<Task<T>> func)
    {
        var dispatcherOperation = _dispatcher.InvokeAsync(func);
        return await (await dispatcherOperation.Task).ConfigureAwait(false);
    }

    public async Task InvokeAsync(Func<Task> func)
    {
        var dispatcherOperation = _dispatcher.InvokeAsync(func);
        await (await dispatcherOperation.Task).ConfigureAwait(false);
    }

    public async Task InvokeAsync(Action action)
    {
        var dispatcherOperation = _dispatcher.InvokeAsync(action);
        await dispatcherOperation.Task.ConfigureAwait(false);
    }

    public void Invoke(Action action)
    {
        if (IsSynchronized)
            action();
        else
            _dispatcher.Invoke(action);
    }
}