using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Moq;
using OpenSubtitleFetcher.Application.Models.Subtitles;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Factory;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Models;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Providers;
using RestEase;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Tests.Providers;

public sealed class OpenSubtitlesProviderTests : IDisposable
{
    // Doesn't exist, so the movie hash can't be computed and only the file name is sent
    private static readonly string MissingFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Film.mkv");

    private readonly StubHttpMessageHandler _handler = new();
    private readonly TempFiles _files = new();
    private readonly OpenSubtitlesProvider _provider;

    public OpenSubtitlesProviderTests()
    {
        var settings = Options.Create(new OpenSubtitlesSettings
        {
            ApiKey = "test-key",
            Url = "https://api.test/api",
            Version = 1,
            UserAgent = "test-agent"
        });

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(OpenSubtitlesClientFactory.HttpClientName))
            .Returns(() => new HttpClient(_handler, disposeHandler: false)
            {
                BaseAddress = new Uri("https://api.test/api/v1")
            });

        _provider = new OpenSubtitlesProvider(new OpenSubtitlesClientFactory(settings, httpClientFactory.Object));
    }

    [Theory]
    [InlineData(Language.Polish, "pl")]
    [InlineData(Language.English, "en")]
    public async Task SearchSubtitlesAsync_SendsFileNameLanguageAndHeaders(Language language, string languageCode)
    {
        // Arrange
        _handler.Respond(HttpStatusCode.OK, """{ "data": [] }""");

        // Act
        await _provider.SearchSubtitlesAsync(MissingFilePath, language);

        // Assert
        var request = Assert.Single(_handler.Requests).Message;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"https://api.test/api/v1/subtitles?languages={languageCode}&query=Film.mkv", request.RequestUri!.ToString());
        Assert.Equal("test-key", Assert.Single(request.Headers.GetValues("Api-Key")));
        Assert.Equal("test-agent", string.Join(" ", request.Headers.GetValues("User-Agent")));
    }

    [Fact]
    public async Task SearchSubtitlesAsync_MapsResponseToCandidates()
    {
        // Arrange
        _handler.Respond(HttpStatusCode.OK, """
            {
              "total_count": 2,
              "data": [
                { "id": "1", "type": "subtitle", "attributes": { "download_count": 15, "files": [ { "file_id": 101 }, { "file_id": 102 } ] } },
                { "id": "2", "type": "subtitle", "attributes": { "download_count": 3, "files": [] } }
              ]
            }
            """);

        // Act
        var result = await _provider.SearchSubtitlesAsync(MissingFilePath, Language.Polish);

        // Assert
        Assert.Collection(result,
            first =>
            {
                Assert.Equal(15, first.DownloadCount);
                Assert.Equal([101, 102], first.Files.Select(x => x.FileId));
            },
            second =>
            {
                Assert.Equal(3, second.DownloadCount);
                Assert.Empty(second.Files);
            });
    }

    [Fact]
    public async Task SearchSubtitlesAsync_MapsTrustTranslationAndHashMatchFlags()
    {
        // Arrange
        _handler.Respond(HttpStatusCode.OK, """
            {
              "data": [
                { "attributes": { "files": [ { "file_id": 1 } ], "from_trusted": true, "machine_translated": true, "moviehash_match": true } },
                { "attributes": { "files": [ { "file_id": 2 } ] } }
              ]
            }
            """);

        // Act
        var result = await _provider.SearchSubtitlesAsync(MissingFilePath, Language.Polish);

        // Assert
        Assert.Collection(result,
            flagged =>
            {
                Assert.True(flagged.FromTrusted);
                Assert.True(flagged.MachineTranslated);
                Assert.True(flagged.MovieHashMatch);
            },
            missingFlags =>
            {
                Assert.False(missingFlags.FromTrusted);
                Assert.False(missingFlags.MachineTranslated);
                Assert.False(missingFlags.MovieHashMatch);
            });
    }

    [Fact]
    public async Task SearchSubtitlesAsync_SendsMovieHashInAlphabeticalOrder_WhenFileCanBeRead()
    {
        // Arrange: a file of zeros hashes to its size (200 000 = 0x30d40)
        var path = _files.Create("Film.mkv", new byte[200_000]);
        _handler.Respond(HttpStatusCode.OK, """{ "data": [] }""");

        // Act
        await _provider.SearchSubtitlesAsync(path, Language.Polish);

        // Assert
        Assert.Equal(
            "https://api.test/api/v1/subtitles?languages=pl&moviehash=0000000000030d40&query=Film.mkv",
            Assert.Single(_handler.Requests).Message.RequestUri!.ToString());
    }

    [Fact]
    public async Task SearchSubtitlesAsync_SearchesByNameOnly_WhenFileIsLocked()
    {
        // Arrange
        var path = _files.Create("Film.mkv", new byte[200_000]);
        await using var lockingStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        _handler.Respond(HttpStatusCode.OK, """{ "data": [] }""");

        // Act
        await _provider.SearchSubtitlesAsync(path, Language.Polish);

        // Assert
        Assert.Equal(
            "https://api.test/api/v1/subtitles?languages=pl&query=Film.mkv",
            Assert.Single(_handler.Requests).Message.RequestUri!.ToString());
    }

    [Fact]
    public async Task DownloadSubtitlesAsync_SendsFileIdAndFileNameAsSnakeCaseJson()
    {
        // Arrange
        _handler.Respond(HttpStatusCode.OK, """{ "link": "https://dl.test/file", "file_name": "Film.srt" }""");

        // Act
        await _provider.DownloadSubtitlesAsync(101, "Film");

        // Assert
        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Message.Method);
        Assert.Equal("https://api.test/api/v1/download", request.Message.RequestUri!.ToString());

        using var body = JsonDocument.Parse(request.Body!);
        Assert.Equal(101, body.RootElement.GetProperty("file_id").GetInt32());
        Assert.Equal("Film", body.RootElement.GetProperty("file_name").GetString());
    }

    [Fact]
    public async Task DownloadSubtitlesAsync_MapsResponse()
    {
        // Arrange
        _handler.Respond(HttpStatusCode.OK, """
            { "link": "https://dl.test/file", "file_name": "Film.srt", "requests": 3, "remaining": 17, "message": "ok" }
            """);

        // Act
        var result = await _provider.DownloadSubtitlesAsync(101, "Film");

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(new SubtitleDownload("https://dl.test/file", "ok", 17, "Film.srt"), result.Value);
    }

    [Fact]
    public async Task DownloadSubtitlesAsync_ReturnsQuotaExceededError_WhenServerRespondsNotAcceptable()
    {
        // Arrange
        _handler.Respond(HttpStatusCode.NotAcceptable, """{ "message": "quota" }""");

        // Act
        var result = await _provider.DownloadSubtitlesAsync(101, "Film");

        // Assert
        Assert.True(result.IsError);
        Assert.Equal((int)SubtitleDownloadErrorCode.QuotaExceeded, result.FirstError.NumericType);
    }

    [Fact]
    public async Task DownloadSubtitlesAsync_ReturnsMissingLinkError_WhenResponseHasNoLink()
    {
        // Arrange
        _handler.Respond(HttpStatusCode.OK, """{ "link": null, "remaining": 5, "message": "something went wrong" }""");

        // Act
        var result = await _provider.DownloadSubtitlesAsync(101, "Film");

        // Assert
        Assert.True(result.IsError);
        Assert.Equal((int)SubtitleDownloadErrorCode.MissingLink, result.FirstError.NumericType);
    }

    [Fact]
    public async Task DownloadSubtitlesAsync_Throws_OnOtherErrorStatusCodes()
    {
        // Arrange
        _handler.Respond(HttpStatusCode.InternalServerError);

        // Act
        var act = () => _provider.DownloadSubtitlesAsync(101, "Film");

        // Assert
        var ex = await Assert.ThrowsAsync<ApiException>(act);
        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
    }

    [Fact]
    public async Task GetSubtitlesFileAsync_ReturnsContentOfLink()
    {
        // Arrange
        _handler.Respond(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });

        // Act
        var result = await _provider.GetSubtitlesFileAsync("https://dl.test/file");

        // Assert
        Assert.Equal([1, 2, 3], result);
        Assert.Equal("https://dl.test/file", Assert.Single(_handler.Requests).Message.RequestUri!.ToString());
    }

    public void Dispose()
    {
        _files.Dispose();
    }
}
