using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Domain.Models.Subtitles.Selection;

namespace OpenSubtitleFetcher.Domain.Tests.Models.Subtitles.Selection;

public class MostDownloadedStrategyTests
{
    private readonly MostDownloadedStrategy _strategy = new();

    [Fact]
    public void Mode_IsMostDownloaded()
    {
        // Act
        var mode = _strategy.Mode;

        // Assert
        Assert.Equal(SubtitleSelectionMode.MostDownloaded, mode);
    }

    [Fact]
    public void Select_ReturnsCandidateWithHighestDownloadCount()
    {
        // Arrange
        var best = Candidates.Create(downloadCount: 500);
        var candidates = new[] { Candidates.Create(downloadCount: 100), best, Candidates.Create(downloadCount: 300) };

        // Act
        var result = _strategy.Select(candidates);

        // Assert
        Assert.Same(best, result);
    }

    [Fact]
    public void Select_IgnoresTrustAndMachineTranslation()
    {
        // Arrange
        var popularMachineTranslated = Candidates.Create(downloadCount: 500, machineTranslated: true);
        var trusted = Candidates.Create(downloadCount: 10, fromTrusted: true, movieHashMatch: true);

        // Act
        var result = _strategy.Select([trusted, popularMachineTranslated]);

        // Assert
        Assert.Same(popularMachineTranslated, result);
    }
}
