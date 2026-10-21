using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Factory;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.RestApi.V1;
using OpenSubtitleFetcher.Infrastructure.RestApi;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles;

public static class DependencyInjectionExtensions
{
    public static void RegisterOpenSubtitlesApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient();
        services.AddSingleton<IOpenSubtitlesClientFactory, OpenSubtitlesClientFactory>();
        services.AddSingleton<IOpenSubtitlesService, OpenSubtitlesService>();
    }
}