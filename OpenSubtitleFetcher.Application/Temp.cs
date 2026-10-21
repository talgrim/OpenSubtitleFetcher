using OpenSubtitleFetcher.Domain.Models.Enums;
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
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}