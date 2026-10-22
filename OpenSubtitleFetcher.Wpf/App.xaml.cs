using OpenSubtitleFetcher.Wpf.DependencyInjection;
using OpenSubtitleFetcher.Wpf.ViewModels;

namespace OpenSubtitleFetcher.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{

    public App()
    {
        CompositeRoot.Initialize();

        var mainWindowVM = CompositeRoot.GetRequiredService<MainWindowViewModel>();

        var mainWindow = new MainWindow
        {
            DataContext = mainWindowVM
        };
        mainWindow.Show();
    }
}

