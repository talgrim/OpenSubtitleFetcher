using ErrorOr;
using Moq;
using OpenSubtitleFetcher.Application.Abstractions.Logging;
using OpenSubtitleFetcher.Application.Abstractions.Providers;
using OpenSubtitleFetcher.Application.Abstractions.Repositories;
using OpenSubtitleFetcher.Application.Models.Enums;
using OpenSubtitleFetcher.Application.Models.Subtitles;
using OpenSubtitleFetcher.Application.UseCases;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Domain.Models.Subtitles;

namespace OpenSubtitleFetcher.Application.Tests.UseCases;

public class DownloadSubtitlesUseCaseTests
{
    private static readonly string FilmPath = Path.Combine("Movies", "Film.mkv");
    private static readonly string OtherFilmPath = Path.Combine("Movies", "Other.mp4");
    private static readonly byte[] Content = [1, 2, 3];

    private readonly Mock<ISubtitlesProvider> _provider = new();
    private readonly Mock<ISubtitleRepository> _repository = new();
    private readonly Mock<ILogDisplay> _logger = new();
    private readonly DownloadSubtitlesUseCase _useCase;

    public DownloadSubtitlesUseCaseTests()
    {
        _provider
            .Setup(x => x.SearchSubtitlesAsync(It.IsAny<string>(), It.IsAny<Language>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Candidate(downloadCount: 10, fileId: 1)]);

        SetupDownload(Download(remaining: 10));

        _provider
            .Setup(x => x.GetSubtitlesFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Content);

        _repository
            .Setup(x => x.SaveAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string path, byte[] _, CancellationToken _) => Path.ChangeExtension(path, ".srt"));

        _useCase = new DownloadSubtitlesUseCase(_provider.Object, _repository.Object, _logger.Object);
    }

    [Fact]
    public async Task DownloadsAndSavesSubtitlesOfMostDownloadedCandidate()
    {
        // Arrange
        _provider
            .Setup(x => x.SearchSubtitlesAsync("Film.mkv", Language.English, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Candidate(10, fileId: 1), Candidate(99, fileId: 2), Candidate(50, fileId: 3)]);

        // Act
        await _useCase.DownloadSubtitlesForFilesAsync([FilmPath], Language.English);

        // Assert
        _provider.Verify(x => x.DownloadSubtitlesAsync(2, "Film", It.IsAny<CancellationToken>()), Times.Once);
        _provider.Verify(x => x.GetSubtitlesFileAsync("https://link", It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(x => x.SaveAsync(FilmPath, Content, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogged("Finished processing 1 file(s)", LogResult.Success);
    }

    [Fact]
    public async Task SkipsFilesWithExistingSubtitles_WhenNotOverwriting()
    {
        // Arrange
        _repository.Setup(x => x.HasSubtitles(FilmPath)).Returns(true);
        _repository.Setup(x => x.HasSubtitles(OtherFilmPath)).Returns(false);

        // Act
        await _useCase.DownloadSubtitlesForFilesAsync([FilmPath, OtherFilmPath], Language.Polish, overwriteExisting: false);

        // Assert
        _provider.Verify(
            x => x.SearchSubtitlesAsync("Film.mkv", It.IsAny<Language>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(x => x.SaveAsync(OtherFilmPath, Content, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogged("Skipped 1 file(s) that already have subtitles", LogResult.Info);
    }

    [Fact]
    public async Task DoesNotCheckExistingSubtitles_WhenOverwriting()
    {
        // Act
        await _useCase.DownloadSubtitlesForFilesAsync([FilmPath], Language.Polish, overwriteExisting: true);

        // Assert
        _repository.Verify(x => x.HasSubtitles(It.IsAny<string>()), Times.Never);
        _repository.Verify(x => x.SaveAsync(FilmPath, Content, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DoesNotDownload_WhenNoSubtitlesFound()
    {
        // Arrange
        _provider
            .Setup(x => x.SearchSubtitlesAsync(It.IsAny<string>(), It.IsAny<Language>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _useCase.DownloadSubtitlesForFilesAsync([FilmPath], Language.Polish);

        // Assert
        _provider.Verify(
            x => x.DownloadSubtitlesAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyLogged("No subtitles found for Film.mkv", LogResult.Warning);
    }

    [Fact]
    public async Task ContinuesWithOtherFiles_WhenDownloadLinkIsMissing()
    {
        // Arrange
        var missingLink = Error.Custom((int)SubtitleDownloadErrorCode.MissingLink, "MissingLink", "No link");
        _provider
            .Setup(x => x.DownloadSubtitlesAsync(It.IsAny<int>(), "Film", It.IsAny<CancellationToken>()))
            .ReturnsAsync(missingLink);

        // Act
        await _useCase.DownloadSubtitlesForFilesAsync([FilmPath, OtherFilmPath], Language.Polish);

        // Assert
        _repository.Verify(x => x.SaveAsync(FilmPath, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(x => x.SaveAsync(OtherFilmPath, Content, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogged("Failed to acquire download link for Film.mkv: No link", LogResult.Error);
    }

    [Fact]
    public async Task ContinuesWithOtherFiles_WhenProviderThrows()
    {
        // Arrange
        _provider
            .Setup(x => x.SearchSubtitlesAsync("Film.mkv", It.IsAny<Language>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network down"));

        // Act
        await _useCase.DownloadSubtitlesForFilesAsync([FilmPath, OtherFilmPath], Language.Polish);

        // Assert
        _repository.Verify(x => x.SaveAsync(OtherFilmPath, Content, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogged("Failed to fetch subtitles for Film.mkv: Network down", LogResult.Error);
    }

    [Fact]
    public async Task StopsProcessing_WhenQuotaIsExceeded()
    {
        // Arrange
        var quotaExceeded = Error.Custom((int)SubtitleDownloadErrorCode.QuotaExceeded, "QuotaExceeded", "Quota reached");
        SetupDownload(quotaExceeded);

        var files = Enumerable.Range(0, 20).Select(i => Path.Combine("Movies", $"Film{i}.mkv")).ToList();

        // Act
        await _useCase.DownloadSubtitlesForFilesAsync(files, Language.Polish);

        // Assert
        // Each worker cancels the run after its first quota error, so at most one file per parallel worker is tried
        _provider.Verify(
            x => x.DownloadSubtitlesAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtMost(3));
        _repository.Verify(
            x => x.SaveAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        VerifyLogged("Download quota exhausted, remaining files were not processed", LogResult.Warning);
    }

    [Fact]
    public async Task StopsProcessing_WhenNoDownloadsRemain()
    {
        // Arrange
        SetupDownload(Download(remaining: 0));

        // Act
        await _useCase.DownloadSubtitlesForFilesAsync([FilmPath], Language.Polish);

        // Assert
        _repository.Verify(x => x.SaveAsync(FilmPath, Content, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogged("Download quota exhausted, remaining files were not processed", LogResult.Warning);
    }

    [Fact]
    public async Task Throws_WhenCancelledByCaller()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => _useCase.DownloadSubtitlesForFilesAsync([FilmPath], Language.Polish, cancellationToken: cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
        _logger.Verify(x => x.Log(It.Is<string>(m => m.Contains("quota")), It.IsAny<LogResult>()), Times.Never);
    }

    private void SetupDownload(ErrorOr<SubtitleDownload> result)
    {
        _provider
            .Setup(x => x.DownloadSubtitlesAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private void VerifyLogged(string message, LogResult result)
    {
        _logger.Verify(x => x.Log(message, result), Times.Once);
    }

    private static SubtitleCandidate Candidate(int downloadCount, int fileId)
    {
        return new SubtitleCandidate(downloadCount, [new SubtitleCandidateFile(fileId)]);
    }

    private static SubtitleDownload Download(int remaining)
    {
        return new SubtitleDownload("https://link", string.Empty, remaining, "Film.srt");
    }
}
