using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Application.Abstractions.Providers;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Factory;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Http;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Models;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Providers;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles;

public static class DependencyInjectionExtensions
{
    public static void RegisterOpenSubtitlesApi(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigSectionNames.OpenSubtitles);
        services.Configure<OpenSubtitlesSettings>(section);

        services.AddSingleton<RequestThrottler>();
        services.AddTransient<ThrottlingHandler>();

        services.AddHttpClient(OpenSubtitlesClientFactory.HttpClientName, c =>
            {
                var config = section.Get<OpenSubtitlesSettings>()!;
                c.BaseAddress = new Uri($"{config.Url}/v{config.Version}");
            })
            .AddHttpMessageHandler<ThrottlingHandler>();
        services.AddSingleton<IOpenSubtitlesClientFactory, OpenSubtitlesClientFactory>();
        services.AddSingleton<ISubtitlesProvider, OpenSubtitlesProvider>();
    }
}