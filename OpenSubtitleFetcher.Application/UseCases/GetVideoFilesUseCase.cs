using OpenSubtitleFetcher.Application.Abstractions.Repositories;
using OpenSubtitleFetcher.Domain.Models.Files;

namespace OpenSubtitleFetcher.Application.UseCases;

public interface IGetVideoFilesUseCase
{
    IReadOnlyCollection<VideoFile> GetVideoFiles(IReadOnlyCollection<string> paths);
}

public class GetVideoFilesUseCase : IGetVideoFilesUseCase
{
    private readonly IVideoFileRepository _videoFileRepository;

    public GetVideoFilesUseCase(IVideoFileRepository videoFileRepository)
    {
        _videoFileRepository = videoFileRepository;
    }

    public IReadOnlyCollection<VideoFile> GetVideoFiles(IReadOnlyCollection<string> paths)
    {
        return _videoFileRepository
            .GetVideoFiles(paths)
            .DistinctBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}