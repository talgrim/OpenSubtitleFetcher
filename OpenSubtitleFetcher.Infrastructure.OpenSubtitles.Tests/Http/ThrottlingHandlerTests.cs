using System.Net;
using System.Net.Http.Headers;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Http;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Tests.Http;

public class ThrottlingHandlerTests
{
    private readonly StubHttpMessageHandler _inner = new();
    private readonly HttpMessageInvoker _invoker;

    public ThrottlingHandlerTests()
    {
        _invoker = new HttpMessageInvoker(new ThrottlingHandler(new RequestThrottler()) { InnerHandler = _inner });
    }

    [Fact]
    public async Task RetriesAfterTooManyRequests()
    {
        // Arrange
        _inner.Respond(TooManyRequests).Respond(HttpStatusCode.OK);

        // Act
        var response = await SendAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, _inner.Requests.Count);
    }

    [Fact]
    public async Task GivesUpAfterMaxRetries()
    {
        // Arrange
        _inner.Respond(TooManyRequests).Respond(TooManyRequests).Respond(TooManyRequests).Respond(TooManyRequests);

        // Act
        var response = await SendAsync();

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(4, _inner.Requests.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.NotAcceptable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task DoesNotRetryOtherStatusCodes(HttpStatusCode statusCode)
    {
        // Arrange
        _inner.Respond(statusCode);

        // Act
        var response = await SendAsync();

        // Assert
        Assert.Equal(statusCode, response.StatusCode);
        Assert.Single(_inner.Requests);
    }

    private Task<HttpResponseMessage> SendAsync()
    {
        return _invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.test/subtitles"), CancellationToken.None);
    }

    private static HttpResponseMessage TooManyRequests()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);

        return response;
    }
}
