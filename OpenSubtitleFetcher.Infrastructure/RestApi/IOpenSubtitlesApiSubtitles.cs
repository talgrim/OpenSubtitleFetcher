using OpenSubtitleFetcher.Infrastructure.Models.OpenSubtitlesApi.Subtitles;
using RestEase;

namespace OpenSubtitleFetcher.Infrastructure.RestApi;

[BasePath("/subtitles")]
public interface IOpenSubtitlesApiSubtitles
{
    [Get("/subtitles?query={query}&languages={languages}")]
    Task<ResponseDto> SearchSubtitlesAsync(
        [Path] string query,
        [Path] string languages,
        CancellationToken cancellationToken = default);
}