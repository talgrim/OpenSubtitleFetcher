using OpenSubtitleFetcher.Domain.Models.Subtitles;

namespace OpenSubtitleFetcher.Domain.Tests.Models.Subtitles;

public class SubtitleCandidateTests
{
    [Fact]
    public void GetBestCandidate_ReturnsCandidateWithHighestDownloadCount()
    {
        // Arrange
        var best = Candidate(downloadCount: 500, fileId: 2);
        var candidates = new[] { Candidate(100, 1), best, Candidate(300, 3) };

        // Act
        var result = SubtitleCandidate.GetBestCandidate(candidates);

        // Assert
        Assert.Same(best, result);
    }

    [Fact]
    public void GetBestCandidate_SkipsCandidatesWithoutFiles()
    {
        // Arrange
        var withoutFiles = new SubtitleCandidate(1000, []);
        var withFiles = Candidate(10, 1);

        // Act
        var result = SubtitleCandidate.GetBestCandidate([withoutFiles, withFiles]);

        // Assert
        Assert.Same(withFiles, result);
    }

    [Fact]
    public void GetBestCandidate_ReturnsNull_WhenNoCandidates()
    {
        // Act
        var result = SubtitleCandidate.GetBestCandidate([]);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetBestCandidate_ReturnsNull_WhenNoCandidateHasFiles()
    {
        // Act
        var result = SubtitleCandidate.GetBestCandidate([new SubtitleCandidate(5, []), new SubtitleCandidate(7, [])]);

        // Assert
        Assert.Null(result);
    }

    private static SubtitleCandidate Candidate(int downloadCount, int fileId)
    {
        return new SubtitleCandidate(downloadCount, [new SubtitleCandidateFile(fileId)]);
    }
}
