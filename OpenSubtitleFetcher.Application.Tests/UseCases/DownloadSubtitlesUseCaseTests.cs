using ErrorOr;
using Moq;
using OpenSubtitleFetcher.Application.Abstractions.Providers;
using OpenSubtitleFetcher.Application.Abstractions.Repositories;
using OpenSubtitleFetcher.Application.Models.Enums;
using OpenSubtitleFetcher.Application.Models.Subtitles;
using OpenSubtitleFetcher.Application.UseCases;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Domain.Models.Subtitles;
using OpenSubtitleFetcher.Domain.Models.Subtitles.Selection;

namespace OpenSubtitleFetcher.Application.Tests.UseCases;

public class DownloadSubtitlesUseCaseTests
{
    private static readonly string FilmPath = Path.Combine("Movies", "Film.mkv");
    private static readonly string OtherFilmPath = Path.Combine("Movies", "Other.mp4");
    private static readonly byte[] Content = [1, 2, 3];

    private static readonly FileDownloadStatus[] FinalStatuses =
    [
        FileDownloadStatus.Skipped,
        FileDownloadStatus.NotFound,
        FileDownloadStatus.Downloaded,
        FileDownloadStatus.Failed,
        FileDownloadStatus.NotProcessed
    ];

    private readonly Mock<ISubtitlesProvider> _provider = new();
    private readonly Mock<ISubtitleRepository> _repository = new();
    private readonly Mock<ISubtitleSelectionStrategy> _bestMatchStrategy = new();
    private readonly Mock<ISubtitleSelectionStrategy> _mostDownloadedStrategy = new();
    private readonly RecordingProgress<FileDownloadProgress> _progress = new();
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
            .Returns(Task.CompletedTask);

        // By default the strategy picks the first candidate, so tests not about selection can ignore it
        SetupStrategy(_bestMatchStrategy, SubtitleSelectionMode.BestMatch);
        SetupStrategy(_mostDownloadedStrategy, SubtitleSelectionMode.MostDownloaded);

        _useCase = new DownloadSubtitlesUseCase(
            _provider.Object,
            _repository.Object,
            [_bestMatchStrategy.Object, _mostDownloadedStrategy.Object]);
    }

    [Fact]
    public async Task DownloadsAndSavesCandidateChosenByStrategy()
    {
        // Arrange
        IReadOnlyCollection<SubtitleCandidate> candidates = [Candidate(10, fileId: 1), Candidate(99, fileId: 2)];
        _provider
            .Setup(x => x.SearchSubtitlesAsync(FilmPath, Language.English, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);
        _bestMatchStrategy.Setup(x => x.Select(candidates)).Returns(candidates.Last());

        // Act
        await RunAsync([FilmPath], Language.English);

        // Assert
        _provider.Verify(x => x.DownloadSubtitlesAsync(2, "Film", It.IsAny<CancellationToken>()), Times.Once);
        _provider.Verify(x => x.GetSubtitlesFileAsync("https://link", It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(x => x.SaveAsync(FilmPath, Content, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(
        [
            new FileDownloadProgress(FilmPath, FileDownloadStatus.Searching),
            new FileDownloadProgress(FilmPath, FileDownloadStatus.Downloading),
            new FileDownloadProgress(FilmPath, FileDownloadStatus.Downloaded, "10 downloads remaining")
        ], ReportsFor(FilmPath));
    }

    [Theory]
    [InlineData(SubtitleSelectionMode.BestMatch)]
    [InlineData(SubtitleSelectionMode.MostDownloaded)]
    public async Task UsesStrategyOfSelectedMode(SubtitleSelectionMode mode)
    {
        // Arrange
        var (selected, other) = mode == SubtitleSelectionMode.BestMatch
            ? (_bestMatchStrategy, _mostDownloadedStrategy)
            : (_mostDownloadedStrategy, _bestMatchStrategy);

        // Act
        await RunAsync([FilmPath], selectionMode: mode);

        // Assert
        selected.Verify(x => x.Select(It.IsAny<IReadOnlyCollection<SubtitleCandidate>>()), Times.Once);
        other.Verify(x => x.Select(It.IsAny<IReadOnlyCollection<SubtitleCandidate>>()), Times.Never);
    }

    [Fact]
    public async Task ReportsNotFound_WhenStrategyRejectsAllCandidates()
    {
        // Arrange
        _bestMatchStrategy
            .Setup(x => x.Select(It.IsAny<IReadOnlyCollection<SubtitleCandidate>>()))
            .Returns((SubtitleCandidate?)null);

        // Act
        await RunAsync([FilmPath]);

        // Assert
        _provider.Verify(
            x => x.DownloadSubtitlesAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(FileDownloadStatus.NotFound, FinalStatusOf(FilmPath));
    }

    [Fact]
    public async Task ReportsSkipped_ForFilesWithExistingSubtitles_WhenNotOverwriting()
    {
        // Arrange
        _repository.Setup(x => x.HasSubtitles(FilmPath)).Returns(true);
        _repository.Setup(x => x.HasSubtitles(OtherFilmPath)).Returns(false);

        // Act
        await RunAsync([FilmPath, OtherFilmPath], overwriteExisting: false);

        // Assert
        _provider.Verify(
            x => x.SearchSubtitlesAsync(FilmPath, It.IsAny<Language>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal([new FileDownloadProgress(FilmPath, FileDownloadStatus.Skipped)], ReportsFor(FilmPath));
        Assert.Equal(FileDownloadStatus.Downloaded, FinalStatusOf(OtherFilmPath));
    }

    [Fact]
    public async Task DoesNotCheckExistingSubtitles_WhenOverwriting()
    {
        // Act
        await RunAsync([FilmPath], overwriteExisting: true);

        // Assert
        _repository.Verify(x => x.HasSubtitles(It.IsAny<string>()), Times.Never);
        Assert.Equal(FileDownloadStatus.Downloaded, FinalStatusOf(FilmPath));
    }

    [Fact]
    public async Task ReportsNotFound_WhenNoSubtitlesFound()
    {
        // Arrange
        _provider
            .Setup(x => x.SearchSubtitlesAsync(It.IsAny<string>(), It.IsAny<Language>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await RunAsync([FilmPath]);

        // Assert
        _provider.Verify(
            x => x.DownloadSubtitlesAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(FileDownloadStatus.NotFound, FinalStatusOf(FilmPath));
    }

    [Theory]
    [InlineData((int)SubtitleDownloadErrorCode.MissingLink)]
    [InlineData(999)]
    public async Task ReportsFailedWithErrorDescription_AndContinues_WhenDownloadFails(int errorCode)
    {
        // Arrange
        _provider
            .Setup(x => x.DownloadSubtitlesAsync(It.IsAny<int>(), "Film", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Custom(errorCode, "Code", "No link"));

        // Act
        await RunAsync([FilmPath, OtherFilmPath]);

        // Assert
        _repository.Verify(x => x.SaveAsync(FilmPath, It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(new FileDownloadProgress(FilmPath, FileDownloadStatus.Failed, "No link"), ReportsFor(FilmPath).Last());
        Assert.Equal(FileDownloadStatus.Downloaded, FinalStatusOf(OtherFilmPath));
    }

    [Fact]
    public async Task ReportsFailedWithExceptionMessage_AndContinues_WhenProviderThrows()
    {
        // Arrange
        _provider
            .Setup(x => x.SearchSubtitlesAsync(FilmPath, It.IsAny<Language>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network down"));

        // Act
        await RunAsync([FilmPath, OtherFilmPath]);

        // Assert
        Assert.Equal(
            new FileDownloadProgress(FilmPath, FileDownloadStatus.Failed, "Network down"),
            ReportsFor(FilmPath).Last());
        Assert.Equal(FileDownloadStatus.Downloaded, FinalStatusOf(OtherFilmPath));
    }

    [Fact]
    public async Task ReportsNotProcessedForEveryFile_WhenQuotaIsExceeded()
    {
        // Arrange
        SetupDownload(Error.Custom((int)SubtitleDownloadErrorCode.QuotaExceeded, "QuotaExceeded", "Quota reached"));
        var files = ManyFiles();

        // Act
        await RunAsync(files);

        // Assert
        // Each worker cancels the run after its first quota error, so at most one file per parallel worker is tried
        _provider.Verify(
            x => x.DownloadSubtitlesAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtMost(3));
        _repository.Verify(
            x => x.SaveAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.All(files, file => Assert.Equal(FileDownloadStatus.NotProcessed, FinalStatusOf(file)));
    }

    [Fact]
    public async Task ReportsRemainingFilesAsNotProcessed_WhenNoDownloadsRemain()
    {
        // Arrange
        SetupDownload(Download(remaining: 0));
        var files = ManyFiles();

        // Act
        await RunAsync(files);

        // Assert
        var finalReports = files.Select(file => ReportsFor(file).Last()).ToList();
        var downloaded = finalReports.Count(x => x.Status == FileDownloadStatus.Downloaded);
        var notProcessed = finalReports
            .Where(x => x.Status == FileDownloadStatus.NotProcessed)
            .ToList();

        Assert.InRange(downloaded, 1, 3);
        Assert.Equal(files.Count - downloaded, notProcessed.Count);
        Assert.All(notProcessed, x => Assert.Equal("Download quota exhausted", x.Details));
        _repository.Verify(
            x => x.SaveAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Exactly(downloaded));
    }

    [Fact]
    public async Task ReportsExactlyOneFinalStatusPerFile()
    {
        // Arrange
        string[] files = [FilmPath, OtherFilmPath, Path.Combine("Movies", "Missing.mkv"), Path.Combine("Movies", "Broken.mkv")];

        _repository.Setup(x => x.HasSubtitles(FilmPath)).Returns(true);
        _provider
            .Setup(x => x.SearchSubtitlesAsync(files[2], It.IsAny<Language>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _provider
            .Setup(x => x.SearchSubtitlesAsync(files[3], It.IsAny<Language>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Broken"));

        // Act
        await RunAsync(files, overwriteExisting: false);

        // Assert
        Assert.All(files, file => Assert.Single(ReportsFor(file), x => FinalStatuses.Contains(x.Status)));
        Assert.Equal(FileDownloadStatus.Skipped, FinalStatusOf(FilmPath));
        Assert.Equal(FileDownloadStatus.Downloaded, FinalStatusOf(OtherFilmPath));
        Assert.Equal(FileDownloadStatus.NotFound, FinalStatusOf(files[2]));
        Assert.Equal(FileDownloadStatus.Failed, FinalStatusOf(files[3]));
    }

    [Fact]
    public async Task Works_WithoutProgress()
    {
        // Act
        await _useCase.DownloadSubtitlesForFilesAsync([FilmPath], Language.Polish, SubtitleSelectionMode.BestMatch);

        // Assert
        _repository.Verify(x => x.SaveAsync(FilmPath, Content, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Throws_WhenCancelledByCaller()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var act = () => _useCase.DownloadSubtitlesForFilesAsync(
            [FilmPath],
            Language.Polish,
            SubtitleSelectionMode.BestMatch,
            progress: _progress,
            cancellationToken: cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
        Assert.Empty(_progress.Reports);
    }

    private Task RunAsync(
        IReadOnlyCollection<string> files,
        Language language = Language.Polish,
        bool overwriteExisting = true,
        SubtitleSelectionMode selectionMode = SubtitleSelectionMode.BestMatch)
    {
        return _useCase.DownloadSubtitlesForFilesAsync(files, language, selectionMode, overwriteExisting, _progress);
    }

    private List<FileDownloadProgress> ReportsFor(string filePath)
    {
        return _progress.Reports.Where(x => x.FilePath == filePath).ToList();
    }

    private FileDownloadStatus FinalStatusOf(string filePath)
    {
        return ReportsFor(filePath).Last().Status;
    }

    private static void SetupStrategy(Mock<ISubtitleSelectionStrategy> strategy, SubtitleSelectionMode mode)
    {
        strategy.Setup(x => x.Mode).Returns(mode);
        strategy
            .Setup(x => x.Select(It.IsAny<IReadOnlyCollection<SubtitleCandidate>>()))
            .Returns((IReadOnlyCollection<SubtitleCandidate> candidates) => candidates.FirstOrDefault());
    }

    private void SetupDownload(ErrorOr<SubtitleDownload> result)
    {
        _provider
            .Setup(x => x.DownloadSubtitlesAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private static List<string> ManyFiles()
    {
        return Enumerable.Range(0, 20).Select(i => Path.Combine("Movies", $"Film{i}.mkv")).ToList();
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
