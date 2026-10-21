using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Infrastructure.Models.OpenSubtitlesApi.Subtitles;
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

    public Task<ResponseDto> SearchSubtitlesAsync(
        string query,
        Language language,
        CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateRestClient();

        return client.SearchSubtitlesAsync(query, _languagesMap[language], cancellationToken);
    }
}