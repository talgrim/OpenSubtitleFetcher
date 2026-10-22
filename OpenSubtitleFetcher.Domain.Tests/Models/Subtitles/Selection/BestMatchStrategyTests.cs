using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Domain.Models.Subtitles.Selection;

namespace OpenSubtitleFetcher.Domain.Tests.Models.Subtitles.Selection;

public class BestMatchStrategyTests
{
    private readonly BestMatchStrategy _strategy = new();

    [Fact]
    public void Mode_IsBestMatch()
    {
        // Act
        var mode = _strategy.Mode;

        // Assert
        Assert.Equal(SubtitleSelectionMode.BestMatch, mode);
    }

    [Fact]
    public void Select_PrefersMovieHashMatchOverEverythingElse()
    {
        // Arrange
        var trustedAndPopular = Candidates.Create(downloadCount: 1000, fromTrusted: true);
        var hashMatch = Candidates.Create(downloadCount: 1, machineTranslated: true, movieHashMatch: true);

        // Act
        var result = _strategy.Select([trustedAndPopular, hashMatch]);

        // Assert
        Assert.Same(hashMatch, result);
    }

    [Fact]
    public void Select_PrefersHumanTranslation_WhenHashMatchIsEqual()
    {
        // Arrange
        var machineTranslated = Candidates.Create(downloadCount: 1000, fromTrusted: true, machineTranslated: true);
        var human = Candidates.Create(downloadCount: 1);

        // Act
        var result = _strategy.Select([machineTranslated, human]);

        // Assert
        Assert.Same(human, result);
    }

    [Fact]
    public void Select_PrefersTrusted_WhenHashMatchAndTranslationAreEqual()
    {
        // Arrange
        var popular = Candidates.Create(downloadCount: 1000);
        var trusted = Candidates.Create(downloadCount: 10, fromTrusted: true);

        // Act
        var result = _strategy.Select([popular, trusted]);

        // Assert
        Assert.Same(trusted, result);
    }

    [Fact]
    public void Select_PrefersMorePopular_WhenOtherCriteriaAreEqual()
    {
        // Arrange
        var lessPopular = Candidates.Create(downloadCount: 10, movieHashMatch: true);
        var morePopular = Candidates.Create(downloadCount: 20, movieHashMatch: true);

        // Act
        var result = _strategy.Select([lessPopular, morePopular]);

        // Assert
        Assert.Same(morePopular, result);
    }
}
