using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Infrastructure.Models.OpenSubtitlesApi;

namespace OpenSubtitleFetcher.Infrastructure.RestApi;

public interface IOpenSubtitlesService
{
    public Task<SearchSubtitlesResponseDto> SearchSubtitlesAsync(
        string query,
        Language language,
        CancellationToken cancellationToken = default);

    public Task<DownloadSubtitlesResponseDto> DownloadSubtitlesAsync(
        int fileId,
        string fileName,
        CancellationToken cancellationToken = default);

    public Task<byte[]> GetSubtitlesFileAsync(
        string link,
        CancellationToken cancellationToken = default);
}