namespace OpenSubtitleFetcher.Application.Abstractions.Repositories;

public interface ISubtitleRepository
{
    bool HasSubtitles(string filePath);

    Task SaveAsync(string filePath, byte[] content, CancellationToken cancellationToken);
}