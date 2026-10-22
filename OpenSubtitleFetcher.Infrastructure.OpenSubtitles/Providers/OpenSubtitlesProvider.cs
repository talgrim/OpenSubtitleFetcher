using System.Net;
using ErrorOr;
using OpenSubtitleFetcher.Application.Abstractions.Providers;
using OpenSubtitleFetcher.Application.Models.Subtitles;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Domain.Models.Subtitles;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Factory;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Mapper;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Models.OpenSubtitlesApi;
using RestEase;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Providers;

internal class OpenSubtitlesProvider : ISubtitlesProvider
{
    private readonly IOpenSubtitlesClientFactory _factory;

    private readonly Dictionary<Language, string> _languagesMap =
        new()
        {
            { Language.English, "en" },
            { Language.Polish, "pl" }
        };

    public OpenSubtitlesProvider(IOpenSubtitlesClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyCollection<SubtitleCandidate>> SearchSubtitlesAsync(
        string query,
        Language language,
        CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateRestClient();

        var response = await client.SearchSubtitlesAsync(query, _languagesMap[language], cancellationToken);

        return response.Data.Select(StaticMapper.ToDomain).ToList();
    }

    public async Task<ErrorOr<SubtitleDownload>> DownloadSubtitlesAsync(
        int fileId,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateRestClient();

        var request = new DownloadSubtitlesRequestDto(fileId, fileName); 

        try
        {
            var response = await client.DownloadSubtitlesAsync(request, cancellationToken);

            if (string.IsNullOrEmpty(response.Link))
            {
                return Error.Custom(
                    (int)SubtitleDownloadErrorCode.MisingLink,
                    nameof(SubtitleDownloadErrorCode.MisingLink),
                    "Subtitles are missing link");
            }

            return StaticMapper.ToDomain(response);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotAcceptable)
        {
            // OpenSubtitles responds with 406 once the daily download quota is used up
            return Error.Custom(
                (int)SubtitleDownloadErrorCode.QuotaExceeded,
                nameof(SubtitleDownloadErrorCode.QuotaExceeded),
                "Daily download quota has been reached");
        }
    }

    public Task<byte[]> GetSubtitlesFileAsync(string link, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateHttpClient();

        return client.GetByteArrayAsync(link, cancellationToken);
    }
}