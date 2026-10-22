using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Application.Abstractions.Repositories;
using OpenSubtitleFetcher.Infrastructure.Repositories;

namespace OpenSubtitleFetcher.Infrastructure;

public static class DependencyInjectionExtensions
{
    public static void RegisterInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IVideoFileRepository, VideoFileRepository>();
        services.AddSingleton<ISubtitleRepository, SubtitleRepository>();
    }
}