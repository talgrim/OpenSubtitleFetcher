using OpenSubtitleFetcher.Application.Logging;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Infrastructure.Logging;
using OpenSubtitleFetcher.Infrastructure.RestApi;

namespace OpenSubtitleFetcher.Application;

public interface ITemp
{
    public Task TestAsync(string query, Language language, CancellationToken cancellationToken = default);
}

public class Temp : ITemp
{
    private readonly IOpenSubtitlesService _service;

    public Temp(IOpenSubtitlesService service)
    {
        _service = service;
    }

    public async Task TestAsync(string query, Language language, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _service.SearchSubtitlesAsync(query, language, cancellationToken);
            LogManager.Log($"Fetched {result.Data.Length} subtitles for {query}", LogResult.Success);
        }
        catch (Exception ex)
        {
            LogManager.Log($"Failed to fetch subtitles for {query}: {ex.Message}", LogResult.Error);
        }
    }
}