using OpenSubtitleFetcher.Application.Abstractions.Repositories;
using OpenSubtitleFetcher.Domain.Models.Subtitles;

namespace OpenSubtitleFetcher.Infrastructure.Repositories;

internal class SubtitleRepository : ISubtitleRepository
{
    public bool HasSubtitles(string filePath)
    {
        return SubtitleFormat.SourceExtensions.Any(x => File.Exists(Path.ChangeExtension(filePath, x)));
    }

    public async Task<string> SaveAsync(
        string filePath,
        byte[] content,
        CancellationToken cancellationToken)
    {
        var subtitlesPath = Path.ChangeExtension(filePath,SubtitleFormat.DefaultExtension);

        await File.WriteAllBytesAsync(subtitlesPath, content, cancellationToken);

        return subtitlesPath;
    }
}