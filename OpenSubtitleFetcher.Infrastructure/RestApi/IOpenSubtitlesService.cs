using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Infrastructure.Models.OpenSubtitlesApi.Subtitles;

namespace OpenSubtitleFetcher.Infrastructure.RestApi;

public interface IOpenSubtitlesService
{
    public Task<ResponseDto> SearchSubtitlesAsync(
        string query,
        Language language,
        CancellationToken cancellationToken = default);
}