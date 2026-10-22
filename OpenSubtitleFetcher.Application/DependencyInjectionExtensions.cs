using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Application.Abstractions.Logging;
using OpenSubtitleFetcher.Application.Logging;
using OpenSubtitleFetcher.Application.UseCases;

namespace OpenSubtitleFetcher.Application;

public static class DependencyInjectionExtensions
{
    public static void RegisterApplication(this IServiceCollection services)
    {
        services.AddScoped<IDownloadSubtitlesUseCase, DownloadSubtitlesUseCase>();
        services.AddSingleton<ILogManager, LogManager>();
    }
}