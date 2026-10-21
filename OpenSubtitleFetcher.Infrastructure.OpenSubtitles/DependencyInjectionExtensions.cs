using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Factory;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Models;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.RestApi.V1;
using OpenSubtitleFetcher.Infrastructure.RestApi;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles;

public static class DependencyInjectionExtensions
{
    public static void RegisterOpenSubtitlesApi(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigSectionNames.OpenSubtitles);
        services.Configure<OpenSubtitlesSettings>(section);

        services.AddHttpClient(OpenSubtitlesClientFactory.HttpClientName, c =>
        {
            var config = section.Get<OpenSubtitlesSettings>()!;
            c.BaseAddress = new Uri($"{config.Url}/v{config.Version}");
        });
        services.AddSingleton<IOpenSubtitlesClientFactory, OpenSubtitlesClientFactory>();
        services.AddSingleton<IOpenSubtitlesService, OpenSubtitlesService>();
    }
}