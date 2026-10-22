using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Application;
using OpenSubtitleFetcher.Application.Abstractions.Logging;
using OpenSubtitleFetcher.Infrastructure;
using OpenSubtitleFetcher.Infrastructure.OpenSubtitles;
using OpenSubtitleFetcher.Wpf.Helpers.UiContexts;
using OpenSubtitleFetcher.Wpf.ViewModels;

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
        UiContext.Initialize(new DefaultContext());

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

        Configuration = builder.Build();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();

        var mainWindowVM = ServiceProvider.GetRequiredService<MainWindowViewModel>();

        var mainWindow = new MainWindow
        {
            DataContext = mainWindowVM
        };
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<LogViewModel>();
        services.AddSingleton<ILogDisplay>(sp => sp.GetRequiredService<LogViewModel>());
        services.AddSingleton<MainWindowViewModel>();

        services.RegisterInfrastructure();
        services.RegisterOpenSubtitlesApi(Configuration);
        services.RegisterApplication();
    }
}

