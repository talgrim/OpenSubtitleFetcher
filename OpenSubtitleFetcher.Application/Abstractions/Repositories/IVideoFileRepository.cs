using OpenSubtitleFetcher.Domain.Models.Files;

namespace OpenSubtitleFetcher.Application.Abstractions.Repositories;

public interface IVideoFileRepository
{
    IReadOnlyCollection<VideoFile> GetVideoFiles(IReadOnlyCollection<string> paths);
}