namespace OpenSubtitleFetcher.Domain.Models.Subtitles;

public record SubtitleCandidate(
    int DownloadCount,
    IReadOnlyCollection<SubtitleCandidateFile> Files,
    bool FromTrusted = false,
    bool MachineTranslated = false,
    double? Fps = null);