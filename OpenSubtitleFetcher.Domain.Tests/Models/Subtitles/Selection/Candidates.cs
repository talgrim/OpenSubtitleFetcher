using OpenSubtitleFetcher.Domain.Models.Subtitles;

namespace OpenSubtitleFetcher.Domain.Tests.Models.Subtitles.Selection;

internal static class Candidates
{
    private static int _nextFileId;

    public static SubtitleCandidate Create(
        int downloadCount = 0,
        bool fromTrusted = false,
        bool machineTranslated = false,
        bool movieHashMatch = false,
        int fileCount = 1)
    {
        var files = Enumerable.Range(0, fileCount)
            .Select(_ => new SubtitleCandidateFile(Interlocked.Increment(ref _nextFileId)))
            .ToList();

        return new SubtitleCandidate(downloadCount, files, fromTrusted, machineTranslated, movieHashMatch);
    }
}
