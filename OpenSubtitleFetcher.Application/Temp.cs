using System.Net;
using OpenSubtitleFetcher.Application.Logging;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Infrastructure.RestApi;
using RestEase;

namespace OpenSubtitleFetcher.Application;

public interface ITemp
{
    public Task TestAsync(
        IReadOnlyCollection<string> filePaths,
        Language language,
        CancellationToken cancellationToken = default);
}

public class Temp : ITemp
{
    // Request rate is capped by the HTTP throttler; parallelism only overlaps network latency
    private const int MaxParallelFiles = 3;

    private static readonly string[] SubtitlesExtensions = [".srt", ".sub", ".ass", ".ssa", ".vtt"];

    private readonly IOpenSubtitlesService _service;

    public Temp(IOpenSubtitlesService service)
    {
        _service = service;
    }

    public async Task TestAsync(
        IReadOnlyCollection<string> filePaths,
        Language language,
        CancellationToken cancellationToken = default)
    {
        var pendingFiles = filePaths.Where(x => !HasSubtitles(x)).ToList();
        var skippedCount = filePaths.Count - pendingFiles.Count;

        if (skippedCount > 0)
        {
            LogManager.Log($"Skipped {skippedCount} file(s) that already have subtitles");
        }

        using var quotaCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxParallelFiles,
            CancellationToken = quotaCts.Token
        };

        try
        {
            await Parallel.ForEachAsync(pendingFiles, options, async (filePath, ct) =>
            {
                var canContinue = await DownloadForFileAsync(filePath, language, ct);

                if (!canContinue)
                {
                    await quotaCts.CancelAsync();
                }
            });

            LogManager.Log($"Finished processing {pendingFiles.Count} file(s)", LogResult.Success);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogManager.Log("Download quota exhausted, remaining files were not processed", LogResult.Warning);
        }
    }

    /// <returns><c>false</c> when the download quota is exhausted and no more files should be processed.</returns>
    private async Task<bool> DownloadForFileAsync(string filePath, Language language, CancellationToken cancellationToken)
    {
        var query = Path.GetFileName(filePath);

        try
        {
            var searchResult = await _service.SearchSubtitlesAsync(query, language, cancellationToken);
            LogManager.Log($"Fetched {searchResult.Data.Length} subtitles for {query}", LogResult.Success);

            var mostDownloadable = searchResult.Data.OrderByDescending(x => x.Attributes!.DownloadCount).FirstOrDefault();

            if (mostDownloadable == null)
            {
                LogManager.Log($"No subtitles found for {query}", LogResult.Warning);
                return true;
            }

            var fileId = mostDownloadable.Attributes!.Files.First().FileId;
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var downloadResult = await _service.DownloadSubtitlesAsync(fileId, fileName, cancellationToken);

            if (string.IsNullOrEmpty(downloadResult.Link))
            {
                LogManager.Log($"Failed to acquire download link for {query}: {downloadResult.Message}", LogResult.Error);
                return downloadResult.Remaining > 0;
            }

            LogManager.Log($"Acquired download link for subtitles {downloadResult.FileName} ({downloadResult.Remaining} downloads remaining)");

            var content = await _service.GetSubtitlesFileAsync(downloadResult.Link, cancellationToken);
            var extension = Path.GetExtension(downloadResult.FileName);
            var subtitlesPath = Path.ChangeExtension(filePath, string.IsNullOrEmpty(extension) ? ".srt" : extension);
            await File.WriteAllBytesAsync(subtitlesPath, content, cancellationToken);
            LogManager.Log($"Saved subtitles to {subtitlesPath}", LogResult.Success);

            return downloadResult.Remaining > 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotAcceptable)
        {
            // OpenSubtitles responds with 406 once the daily download quota is used up
            LogManager.Log($"Failed to fetch subtitles for {query}: {ex.Content}", LogResult.Error);
            return false;
        }
        catch (Exception ex)
        {
            LogManager.Log($"Failed to fetch subtitles for {query}: {ex.Message}", LogResult.Error);
            return true;
        }
    }

    private static bool HasSubtitles(string filePath)
    {
        return SubtitlesExtensions.Any(x => File.Exists(Path.ChangeExtension(filePath, x)));
    }
}
