using OpenSubtitleFetcher.Domain.Models.Subtitles;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Models.OpenSubtitlesApi;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Mapper;

internal static class StaticMapper
{
    public static SubtitleCandidate ToDomain(DataDto response)
    {
        return new SubtitleCandidate(
            response.Attributes!.DownloadCount,
            response.Attributes.Files.Select(ToDomain).ToList());
    }

    private static SubtitleCandidateFile ToDomain(FileDto response)
    {
        return new SubtitleCandidateFile(response.FileId);
    }

    public static SubtitleDownload ToDomain(DownloadSubtitlesResponseDto response)
    {
        return new SubtitleDownload(response.Link, response.Message, response.Remaining, response.FileName);
    }
}