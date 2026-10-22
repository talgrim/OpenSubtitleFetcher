using OpenSubtitleFetcher.Application.Abstractions.Logging;
using OpenSubtitleFetcher.Application.Models.Enums;
using OpenSubtitleFetcher.Domain.Models.Enums;

namespace OpenSubtitleFetcher.Application.Logging;

public class LogManager : ILogManager
{
    private static ILogDisplay _logDisplay = null!;

    public void Initialize(ILogDisplay logDisplay)
    {
        _logDisplay = logDisplay;
    }

    public void Log(string message, LogResult result = LogResult.Info)
    {
        _logDisplay.Log(message, result);
    }
}