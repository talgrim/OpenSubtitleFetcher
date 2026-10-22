using OpenSubtitleFetcher.Application.Abstractions.Logging;
using OpenSubtitleFetcher.Application.Abstractions.Providers;
using OpenSubtitleFetcher.Application.Abstractions.Repositories;
using OpenSubtitleFetcher.Application.Models.Enums;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Domain.Models.Subtitles;

namespace OpenSubtitleFetcher.Application.UseCases;

public interface IDownloadSubtitlesUseCase
{
    public Task DownloadSubtitlesForFilesAsync(
        IReadOnlyCollection<string> filePaths,
        Language language,
        bool overwriteExisting = true,
        CancellationToken cancellationToken = default);
}

public class DownloadSubtitlesUseCase : IDownloadSubtitlesUseCase
{
    // Request rate is capped by the HTTP throttler; parallelism only overlaps network latency
    private const int MaxParallelFiles = 3;

    private readonly ISubtitlesProvider _provider;
    private readonly ISubtitleRepository _subtitleRepository;
    private readonly ILogDisplay _logger;

    public DownloadSubtitlesUseCase(
        ISubtitlesProvider provider,
        ISubtitleRepository subtitleRepository,
        ILogDisplay logger)
    {
        _provider = provider;
        _subtitleRepository = subtitleRepository;
        _logger = logger;
    }

    public async Task DownloadSubtitlesForFilesAsync(
        IReadOnlyCollection<string> filePaths,
        Language language,
        bool overwriteExisting = true,
        CancellationToken cancellationToken = default)
    {
        var pendingFiles = filePaths;

        if (!overwriteExisting)
        {
            pendingFiles = pendingFiles.Where(x => !_subtitleRepository.HasSubtitles(x)).ToList();
            var skippedCount = filePaths.Count - pendingFiles.Count;

            if (skippedCount > 0)
            {
                _logger.Log($"Skipped {skippedCount} file(s) that already have subtitles");
            }
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

            _logger.Log($"Finished processing {pendingFiles.Count} file(s)", LogResult.Success);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.Log("Download quota exhausted, remaining files were not processed", LogResult.Warning);
        }
    }

    /// <returns><c>false</c> when the download quota is exhausted and no more files should be processed.</returns>
    private async Task<bool> DownloadForFileAsync(string filePath, Language language, CancellationToken cancellationToken)
    {
        var query = Path.GetFileName(filePath);

        try
        {
            var candidates = await _provider.SearchSubtitlesAsync(query, language, cancellationToken);
            _logger.Log($"Fetched {candidates.Count} subtitles for {query}", LogResult.Success);

            var bestCandidate = SubtitleCandidate.GetBestCandidate(candidates);

            if (bestCandidate == null)
            {
                _logger.Log($"No subtitles found for {query}", LogResult.Warning);
                return true;
            }

            var fileId = bestCandidate.Files.First().FileId;
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var downloadResult = await _provider.DownloadSubtitlesAsync(fileId, fileName, cancellationToken);

            if (downloadResult.IsError)
            {
                _logger.Log(
                    $"Failed to acquire download link for {query}: {downloadResult.FirstError.Description}",
                    LogResult.Error);

                return false;
            }

            var result = downloadResult.Value;
            if (string.IsNullOrEmpty(result.Link))
            {
                _logger.Log($"Failed to acquire download link for {query}: {result.Message}", LogResult.Error);
                return result.Remaining > 0;
            }

            _logger.Log($"Acquired download link for subtitles {result.FileName} ({result.Remaining} downloads remaining)");

            var content = await _provider.GetSubtitlesFileAsync(result.Link, cancellationToken);

            var subtitlesPath = await _subtitleRepository.SaveAsync(filePath, content, cancellationToken);

            _logger.Log($"Saved subtitles to {subtitlesPath}", LogResult.Success);

            return result.Remaining > 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Log($"Failed to fetch subtitles for {query}: {ex.Message}", LogResult.Error);
            return true;
        }
    }
}
