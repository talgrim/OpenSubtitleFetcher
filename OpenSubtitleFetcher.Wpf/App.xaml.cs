using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Wpf.DependencyInjection;
using OpenSubtitleFetcher.Wpf.ViewModels;

namespace OpenSubtitleFetcher.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _services = CompositionRoot.Build();

        var mainWindow = new MainWindow
        {
            DataContext = _services.GetRequiredService<MainWindowViewModel>()
        };

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }
}

