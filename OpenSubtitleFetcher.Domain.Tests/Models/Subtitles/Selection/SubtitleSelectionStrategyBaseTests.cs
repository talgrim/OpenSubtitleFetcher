using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Domain.Models.Subtitles.Selection;

namespace OpenSubtitleFetcher.Domain.Tests.Models.Subtitles.Selection;

/// <summary>
/// Rules shared by every strategy through <see cref="SubtitleSelectionStrategyBase"/>.
/// </summary>
public class SubtitleSelectionStrategyBaseTests
{
    private static readonly ISubtitleSelectionStrategy[] AllStrategies =
    [
        new MostDownloadedStrategy(),
        new TrustedFirstStrategy(),
        new BestMatchStrategy()
    ];

    public static TheoryData<ISubtitleSelectionStrategy> Strategies => new(AllStrategies);

    [Theory]
    [MemberData(nameof(Strategies))]
    public void Select_ReturnsNull_WhenNoCandidates(ISubtitleSelectionStrategy strategy)
    {
        // Act
        var result = strategy.Select([]);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void Select_SkipsCandidatesWithoutFiles(ISubtitleSelectionStrategy strategy)
    {
        // Arrange
        var withoutFiles = Candidates.Create(downloadCount: 1000, fromTrusted: true, movieHashMatch: true, fileCount: 0);
        var withFile = Candidates.Create(downloadCount: 1);

        // Act
        var result = strategy.Select([withoutFiles, withFile]);

        // Assert
        Assert.Same(withFile, result);
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void Select_SkipsMultiFileCandidates(ISubtitleSelectionStrategy strategy)
    {
        // Arrange
        var multiCd = Candidates.Create(downloadCount: 1000, fromTrusted: true, movieHashMatch: true, fileCount: 2);
        var singleFile = Candidates.Create(downloadCount: 1);

        // Act
        var result = strategy.Select([multiCd, singleFile]);

        // Assert
        Assert.Same(singleFile, result);
    }

    [Theory]
    [MemberData(nameof(Strategies))]
    public void Select_ReturnsNull_WhenOnlyMultiFileCandidates(ISubtitleSelectionStrategy strategy)
    {
        // Arrange
        var candidates = new[] { Candidates.Create(fileCount: 2), Candidates.Create(fileCount: 3) };

        // Act
        var result = strategy.Select(candidates);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void EveryStrategyHasDistinctMode()
    {
        // Act
        var modes = AllStrategies.Select(x => x.Mode).ToList();

        // Assert
        Assert.Equal(modes.Count, modes.Distinct().Count());
        Assert.Equal(Enum.GetValues<SubtitleSelectionMode>().Order(), modes.Order());
    }
}
