using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Application;
using OpenSubtitleFetcher.Domain;
using OpenSubtitleFetcher.Infrastructure;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles;
using OpenSubtitleFetcher.Wpf.Helpers.UiContexts;

namespace OpenSubtitleFetcher.Wpf.DependencyInjection;

public static class CompositionRoot
{
    public static ServiceProvider Build()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

        var configuration = builder.Build();

        var serviceCollection = new ServiceCollection();

        ConfigureServices(serviceCollection, configuration);

        return serviceCollection.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IUiContext, DefaultContext>();
        services.RegisterDomain();
        services.RegisterWpf();
        services.RegisterInfrastructure();
        services.RegisterOpenSubtitlesApi(configuration);
        services.RegisterApplication();
    }
}