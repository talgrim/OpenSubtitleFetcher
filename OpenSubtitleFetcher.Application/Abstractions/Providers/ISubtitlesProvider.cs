using ErrorOr;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Domain.Models.Subtitles;

namespace OpenSubtitleFetcher.Application.Abstractions.Providers;

public interface ISubtitlesProvider
{
    public Task<IReadOnlyCollection<SubtitleCandidate>> SearchSubtitlesAsync(
        string query,
        Language language,
        CancellationToken cancellationToken = default);

    public Task<ErrorOr<SubtitleDownload>> DownloadSubtitlesAsync(
        int fileId,
        string fileName,
        CancellationToken cancellationToken = default);

    public Task<byte[]> GetSubtitlesFileAsync(
        string link,
        CancellationToken cancellationToken = default);
}