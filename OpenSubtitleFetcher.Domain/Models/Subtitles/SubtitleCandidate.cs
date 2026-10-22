namespace OpenSubtitleFetcher.Domain.Models.Subtitles;

public record SubtitleCandidate(int DownloadCount, IReadOnlyCollection<SubtitleCandidateFile> Files)
{
    public static SubtitleCandidate? GetBestCandidate(IReadOnlyCollection<SubtitleCandidate> subtitleCandidates)
    {
        return subtitleCandidates.Where(x => x.Files.Count > 0).MaxBy(x => x.DownloadCount);
    }
}