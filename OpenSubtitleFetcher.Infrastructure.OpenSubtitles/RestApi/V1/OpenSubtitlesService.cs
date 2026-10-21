using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Infrastructure.Models.OpenSubtitlesApi;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Factory;
using OpenSubtitleFetcher.Infrastructure.RestApi;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.RestApi.V1;

internal class OpenSubtitlesService : OpenSubtitlesServiceBase, IOpenSubtitlesService
{
    private readonly IOpenSubtitlesClientFactory _factory;

    private readonly Dictionary<Language, string> _languagesMap =
        new()
        {
            { Language.English, "en" },
            { Language.Polish, "pl" }
        };

    public OpenSubtitlesService(IOpenSubtitlesClientFactory factory)
    {
        _factory = factory;
    }

    public Task<SearchSubtitlesResponseDto> SearchSubtitlesAsync(
        string query,
        Language language,
        CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateRestClient();

        return client.SearchSubtitlesAsync(query, _languagesMap[language], cancellationToken);
    }

    public Task<DownloadSubtitlesResponseDto> DownloadSubtitlesAsync(
        int fileId,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateRestClient();

        return client.DownloadSubtitlesAsync(new DownloadSubtitlesRequestDto(fileId, fileName), cancellationToken);
    }

    public Task<byte[]> GetSubtitlesFileAsync(string link, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateHttpClient();

        return client.GetByteArrayAsync(link, cancellationToken);
    }
}