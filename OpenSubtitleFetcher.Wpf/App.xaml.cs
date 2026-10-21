using System.CodeDom.Compiler;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Application;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles;
using OpenSubtitleFetcher.Infrastructure.RestApi;
using OpenSubtitleFetcher.Wpf.Models;

namespace OpenSubtitleFetcher.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public IServiceProvider ServiceProvider { get; private set; }
    public IConfiguration Configuration { get; }

    public App()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        Configuration = builder.Build();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.Configure<OpenSubtitlesSettings>(Configuration.GetSection(ConfigSectionNames.OpenSubtitles));

        services.RegisterOpenSubtitlesApi(Configuration);
        services.AddScoped<ITemp, Temp>();

        services.AddTransient<MainWindow>();
    }
}

