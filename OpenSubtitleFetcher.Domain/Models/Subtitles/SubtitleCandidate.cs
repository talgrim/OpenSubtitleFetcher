namespace OpenSubtitleFetcher.Domain.Models.Subtitles;

public record SubtitleCandidate(int DownloadCount, IReadOnlyCollection<SubtitleCandidateFile> Files)
{
    public static SubtitleCandidate? GetBestCandidate(IReadOnlyCollection<SubtitleCandidate> subtitleCandidates)
    {
        return subtitleCandidates.MaxBy(x => x.DownloadCount);
    }
}