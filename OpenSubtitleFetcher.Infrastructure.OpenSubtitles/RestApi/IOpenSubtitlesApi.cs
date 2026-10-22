using System.Runtime.CompilerServices;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Models.OpenSubtitlesApi;
using RestEase;

[assembly: InternalsVisibleTo(RestClient.FactoryAssemblyName)]

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.RestApi;

internal interface IOpenSubtitlesApi
{
    [Header("Api-Key")]
    string ApiKey { get; set; }

    [Header("User-Agent")]
    string UserAgent { get; set; }

    [Get("/subtitles")]
    Task<SearchSubtitlesResponseDto> SearchSubtitlesAsync(
        string query,
        string languages,
        CancellationToken cancellationToken = default);

    [Post("/download")]
    [Header("Content-Type",  "application/json")]
    [Header("Accept", "application/json")]
    Task<DownloadSubtitlesResponseDto> DownloadSubtitlesAsync(
        [Body] DownloadSubtitlesRequestDto request,
        CancellationToken cancellationToken = default);
}