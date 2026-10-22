using Microsoft.Extensions.DependencyInjection;
using OpenSubtitleFetcher.Application.Abstractions.Logging;
using OpenSubtitleFetcher.Wpf.Helpers.FolderPicker;
using OpenSubtitleFetcher.Wpf.ViewModels;

namespace OpenSubtitleFetcher.Wpf.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static void RegisterWpf(this IServiceCollection services)
    {
        services.AddSingleton<LogViewModel>();
        services.AddSingleton<ILogDisplay>(sp => sp.GetRequiredService<LogViewModel>());
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<IFolderPicker, FolderPicker>();
    }
}