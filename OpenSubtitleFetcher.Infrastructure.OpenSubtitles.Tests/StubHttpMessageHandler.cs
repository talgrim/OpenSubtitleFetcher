using System.Net;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Tests;

/// <summary>
/// Returns canned responses and records every request (with its body read eagerly, since the
/// request content is disposed once the client is done with it).
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    public List<RecordedRequest> Requests { get; } = [];

    public StubHttpMessageHandler Respond(HttpStatusCode statusCode, string content = "")
    {
        _responses.Enqueue(() => new HttpResponseMessage(statusCode) { Content = new StringContent(content) });
        return this;
    }

    public StubHttpMessageHandler Respond(Func<HttpResponseMessage> response)
    {
        _responses.Enqueue(response);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new RecordedRequest(request, body));

        if (!_responses.TryDequeue(out var response))
        {
            throw new InvalidOperationException($"No response configured for {request.Method} {request.RequestUri}");
        }

        return response();
    }
}

internal record RecordedRequest(HttpRequestMessage Message, string? Body);
