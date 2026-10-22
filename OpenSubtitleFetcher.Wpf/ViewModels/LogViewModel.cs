using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenSubtitleFetcher.Application.Abstractions.Logging;
using OpenSubtitleFetcher.Application.Models.Enums;
using OpenSubtitleFetcher.Wpf.Helpers.UiContexts;

namespace OpenSubtitleFetcher.Wpf.ViewModels;

public partial class LogViewModel : ObservableObject, ILogDisplay
{
    private readonly IUiContext _uiContext;
    private readonly StringBuilder _stringBuilder = new();

    [ObservableProperty] public partial string LogBlock { get; set; } = string.Empty;

    public LogViewModel(IUiContext uiContext)
    {
        _uiContext = uiContext;
    }

    public void Log(string message, LogResult logLevel = LogResult.Info)
    {
        string log;

        // Log is called from parallel downloads
        lock (_stringBuilder)
        {
            _stringBuilder.AppendLine($"{DateTime.Now}: [{logLevel}] - {message}");
            log = _stringBuilder.ToString();
        }

        _uiContext.InvokeAsync(() => LogBlock = log);
    }
}
