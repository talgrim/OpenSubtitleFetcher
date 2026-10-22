using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Application;
using OpenSubtitleFetcher.Infrastructure;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles;
using OpenSubtitleFetcher.Wpf.Helpers.UiContexts;

namespace OpenSubtitleFetcher.Wpf.DependencyInjection;

public class CompositeRoot
{
    private static IServiceProvider ServiceProvider { get; set; } = null!;
    private static IConfiguration Configuration { get; set;  } = null!;

    public static void Initialize()
    {
        UiContext.Initialize(new DefaultContext());

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

        Configuration = builder.Build();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();

    }

    public static T GetRequiredService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.RegisterWpf();
        services.RegisterInfrastructure();
        services.RegisterOpenSubtitlesApi(Configuration);
        services.RegisterApplication();
    }
}