using System.Net;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Http;

/// <summary>
/// Sends every request through <see cref="RequestThrottler"/> and retries after the delay
/// the server asks for when it responds with 429 Too Many Requests.
/// </summary>
internal class ThrottlingHandler : DelegatingHandler
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(1);

    private readonly RequestThrottler _throttler;

    public ThrottlingHandler(RequestThrottler throttler)
    {
        _throttler = throttler;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            await _throttler.WaitAsync(cancellationToken);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= MaxRetries)
            {
                return response;
            }

            var retryDelay = GetRetryDelay(response);
            response.Dispose();

            // Pausing the shared throttler holds back the other parallel requests too
            await _throttler.PauseAsync(retryDelay, cancellationToken);
        }
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter?.Delta is { } delta)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date && date > DateTimeOffset.UtcNow)
        {
            return date - DateTimeOffset.UtcNow;
        }

        return DefaultRetryDelay;
    }
}
