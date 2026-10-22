using OpenSubtitleFetcher.Application.Abstractions.Repositories;
using OpenSubtitleFetcher.Domain.Models.Subtitles;

namespace OpenSubtitleFetcher.Infrastructure.Repositories;

internal class SubtitleRepository : ISubtitleRepository
{
    public bool HasSubtitles(string filePath)
    {
        return SubtitleFormat.SourceExtensions.Any(x => File.Exists(Path.ChangeExtension(filePath, x)));
    }

    public Task SaveAsync(
        string filePath,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var subtitlesPath = Path.ChangeExtension(filePath,SubtitleFormat.DefaultExtension);

        return File.WriteAllBytesAsync(subtitlesPath, content, cancellationToken);
    }
}