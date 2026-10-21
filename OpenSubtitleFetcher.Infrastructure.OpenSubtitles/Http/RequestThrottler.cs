namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Http;

/// <summary>
/// Spaces out requests to OpenSubtitles across all clients, so the app stays below the API rate limit
/// (5 requests per second per IP).
/// </summary>
internal class RequestThrottler
{
    private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(250);

    private readonly SemaphoreSlim _lock = new(1, 1);
    private DateTime _nextAllowedUtc = DateTime.MinValue;

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var delay = _nextAllowedUtc - DateTime.UtcNow;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);
            }

            _nextAllowedUtc = DateTime.UtcNow + MinInterval;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PauseAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var resumeUtc = DateTime.UtcNow + duration;

            if (resumeUtc > _nextAllowedUtc)
            {
                _nextAllowedUtc = resumeUtc;
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
