using System.Diagnostics;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Http;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Tests.Http;

public class RequestThrottlerTests
{
    // Throttler spaces requests 250 ms apart; small margin for timer resolution
    private static readonly TimeSpan MinExpectedInterval = TimeSpan.FromMilliseconds(200);

    private readonly RequestThrottler _throttler = new();

    [Fact]
    public async Task WaitAsync_FirstCallDoesNotWait()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _throttler.WaitAsync(CancellationToken.None);

        // Assert
        Assert.True(stopwatch.Elapsed < MinExpectedInterval, $"First call took {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task WaitAsync_SpacesOutConsecutiveCalls()
    {
        // Arrange
        await _throttler.WaitAsync(CancellationToken.None);
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _throttler.WaitAsync(CancellationToken.None);
        await _throttler.WaitAsync(CancellationToken.None);

        // Assert
        Assert.True(stopwatch.Elapsed >= 2 * MinExpectedInterval, $"Two calls took only {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task PauseAsync_DelaysNextCall()
    {
        // Arrange
        await _throttler.PauseAsync(TimeSpan.FromMilliseconds(600), CancellationToken.None);
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _throttler.WaitAsync(CancellationToken.None);

        // Assert
        Assert.True(stopwatch.Elapsed >= TimeSpan.FromMilliseconds(500), $"Call after pause took only {stopwatch.Elapsed}");
    }

    [Fact]
    public async Task WaitAsync_ThrowsWhenCancelledDuringDelay()
    {
        // Arrange
        await _throttler.PauseAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        var act = () => _throttler.WaitAsync(cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
    }
}
