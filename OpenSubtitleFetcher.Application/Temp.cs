using OpenSubtitleFetcher.Application.Logging;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Infrastructure.RestApi;

namespace OpenSubtitleFetcher.Application;

public interface ITemp
{
    public Task TestAsync(string filePath, Language language, CancellationToken cancellationToken = default);
}

public class Temp : ITemp
{
    private readonly IOpenSubtitlesService _service;

    public Temp(IOpenSubtitlesService service)
    {
        _service = service;
    }

    public async Task TestAsync(string filePath, Language language, CancellationToken cancellationToken = default)
    {
        var query = Path.GetFileName(filePath);

        try
        {
            var searchResult = await _service.SearchSubtitlesAsync(query, language, cancellationToken);
            LogManager.Log($"Fetched {searchResult.Data.Length} subtitles for {query}", LogResult.Success);

            var mostDownloadable = searchResult.Data.OrderByDescending(x => x.Attributes!.DownloadCount).FirstOrDefault();

            if (mostDownloadable == null)
            {
                return;
            }

            var fileId = mostDownloadable.Attributes!.Files.First().FileId;
            var fileName = query[..^4];
            var downloadResult = await _service.DownloadSubtitlesAsync(fileId, fileName, cancellationToken);

            if (string.IsNullOrEmpty(downloadResult.Link))
            {
                LogManager.Log($"Failed to acquire download link for {query}: {downloadResult.Message}", LogResult.Error);
                return;
            }

            LogManager.Log($"Acquired download link for subtitles {downloadResult.FileName}");

            var content = await _service.GetSubtitlesFileAsync(downloadResult.Link, cancellationToken);
            var extension = Path.GetExtension(downloadResult.FileName);
            var subtitlesPath = Path.ChangeExtension(filePath, string.IsNullOrEmpty(extension) ? ".srt" : extension);
            await File.WriteAllBytesAsync(subtitlesPath, content, cancellationToken);
            LogManager.Log($"Saved subtitles to {subtitlesPath}", LogResult.Success);
        }
        catch (Exception ex)
        {
            LogManager.Log($"Failed to fetch subtitles for {query}: {ex.Message}", LogResult.Error);
        }
    }
}