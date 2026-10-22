using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Application.UseCases;

namespace OpenSubtitleFetcher.Application;

public static class DependencyInjectionExtensions
{
    public static void RegisterApplication(this IServiceCollection services)
    {
        services.AddTransient<IDownloadSubtitlesUseCase, DownloadSubtitlesUseCase>();
        services.AddTransient<IGetVideoFilesUseCase, GetVideoFilesUseCase>();
    }
}