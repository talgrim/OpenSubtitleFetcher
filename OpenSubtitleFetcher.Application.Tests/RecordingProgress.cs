using System.Collections.Concurrent;

namespace OpenSubtitleFetcher.Application.Tests;

/// <summary>
/// Records reports synchronously. <see cref="Progress{T}"/> can't be used in tests, because without
/// a synchronization context it raises its callbacks on the thread pool, after the call under test returns.
/// </summary>
internal sealed class RecordingProgress<T> : IProgress<T>
{
    private readonly ConcurrentQueue<T> _reports = new();

    public IReadOnlyCollection<T> Reports => _reports;

    public void Report(T value)
    {
        _reports.Enqueue(value);
    }
}
