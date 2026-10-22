using System.Collections.Concurrent;
using OpenSubtitleFetcher.Application.Abstractions.Repositories;
using OpenSubtitleFetcher.Domain.Models.Files;

namespace OpenSubtitleFetcher.Infrastructure.Repositories;

internal class VideoFileRepository : IVideoFileRepository
{
    public IReadOnlyCollection<VideoFile> GetVideoFiles(IReadOnlyCollection<string> paths)
    {
        var bag = new ConcurrentBag<VideoFile>();

        Parallel.ForEach(paths, path =>
        {
            var directoryInfo = new DirectoryInfo(path);
            var files = directoryInfo
                .EnumerateFiles()
                .Where(x => VideoFile.Extensions.Contains(x.Extension,  StringComparer.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                bag.Add(new VideoFile(file.FullName, file.Name));
            }
        });

        return bag.ToList();
    }
}