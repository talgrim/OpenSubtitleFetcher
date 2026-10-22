using System.Collections.Concurrent;
using OpenSubtitleFetcher.Application.Abstractions.Providers;
using OpenSubtitleFetcher.Application.Abstractions.Repositories;
using OpenSubtitleFetcher.Application.Models.Enums;
using OpenSubtitleFetcher.Application.Models.Subtitles;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Domain.Models.Subtitles;

namespace OpenSubtitleFetcher.Application.UseCases;

public interface IDownloadSubtitlesUseCase
{
    public Task DownloadSubtitlesForFilesAsync(
        IReadOnlyCollection<string> filePaths,
        Language language,
        bool overwriteExisting = true,
        IProgress<FileDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public class DownloadSubtitlesUseCase : IDownloadSubtitlesUseCase
{
    // Request rate is capped by the HTTP throttler; parallelism only overlaps network latency
    private const int MaxParallelFiles = 3;

    private readonly ISubtitlesProvider _provider;
    private readonly ISubtitleRepository _subtitleRepository;

    public DownloadSubtitlesUseCase(
        ISubtitlesProvider provider,
        ISubtitleRepository subtitleRepository)
    {
        _provider = provider;
        _subtitleRepository = subtitleRepository;
    }

    public async Task DownloadSubtitlesForFilesAsync(
        IReadOnlyCollection<string> filePaths,
        Language language,
        bool overwriteExisting = true,
        IProgress<FileDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var pendingFiles = new ConcurrentDictionary<string, FileDownloadStatus>(
            filePaths.ToDictionary(x => x, _ => FileDownloadStatus.Pending));

        if (!overwriteExisting)
        {
            foreach (var filePath in filePaths.Where(x => _subtitleRepository.HasSubtitles(x)))
            {
                pendingFiles[filePath] = FileDownloadStatus.Skipped;
                progress?.Report(new FileDownloadProgress(filePath, FileDownloadStatus.Skipped));
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
            await Parallel.ForEachAsync(pendingFiles.Where(x => x.Value == FileDownloadStatus.Pending), options,
                async (filePath, ct) =>
                {
                    // CancelAsync propagates to the loop's token asynchronously, so check the source directly
                    if (quotaCts.IsCancellationRequested)
                    {
                        return;
                    }

                    pendingFiles[filePath.Key] = FileDownloadStatus.Searching;

                    var canContinue = await DownloadForFileAsync(filePath.Key, language, progress, ct);

                    if (!canContinue)
                    {
                        await quotaCts.CancelAsync();
                    }
                });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Quota exhausted, remaining files are reported below
        }

        foreach (var (path, _) in pendingFiles.Where(x => x.Value == FileDownloadStatus.Pending))
        {
            progress?.Report(
                new FileDownloadProgress(path, FileDownloadStatus.NotProcessed, "Download quota exhausted"));
        }
    }

    /// <returns><c>false</c> when the download quota is exhausted and no more files should be processed.</returns>
    private async Task<bool> DownloadForFileAsync(
        string filePath,
        Language language,
        IProgress<FileDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var query = Path.GetFileName(filePath);

        try
        {
            Report(FileDownloadStatus.Searching);

            var candidates = await _provider.SearchSubtitlesAsync(query, language, cancellationToken);

            var bestCandidate = SubtitleCandidate.GetBestCandidate(candidates);

            if (bestCandidate == null)
            {
                Report(FileDownloadStatus.NotFound);
                return true;
            }

            var fileId = bestCandidate.Files.First().FileId;
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var downloadResult = await _provider.DownloadSubtitlesAsync(fileId, fileName, cancellationToken);

            if (downloadResult.IsError)
            {
                switch ((SubtitleDownloadErrorCode)downloadResult.FirstError.NumericType)
                {
                    case SubtitleDownloadErrorCode.MissingLink:
                        Report(FileDownloadStatus.Failed, downloadResult.FirstError.Description);
                        return true;
                    case SubtitleDownloadErrorCode.QuotaExceeded:
                        Report(FileDownloadStatus.NotProcessed, downloadResult.FirstError.Description);
                        return false;
                    default:
                        Report(FileDownloadStatus.Failed, downloadResult.FirstError.Description);
                        return true;
                }
            }

            var result = downloadResult.Value;

            Report(FileDownloadStatus.Downloading);

            var content = await _provider.GetSubtitlesFileAsync(result.Link, cancellationToken);

            await _subtitleRepository.SaveAsync(filePath, content, cancellationToken);

            Report(FileDownloadStatus.Downloaded, $"{result.Remaining} downloads remaining");

            return result.Remaining > 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Report(FileDownloadStatus.Failed, ex.Message);
            return true;
        }

        void Report(FileDownloadStatus status, string? details = null)
        {
            progress?.Report(new FileDownloadProgress(filePath, status, details));
        }
    }
}
