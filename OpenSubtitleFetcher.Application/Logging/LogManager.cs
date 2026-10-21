using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Infrastructure.Logging;

namespace OpenSubtitleFetcher.Application.Logging;

public static class LogManager
{
    private static ILogDisplay _logDisplay = null!;

    public static void Initialize(ILogDisplay logDisplay)
    {
        _logDisplay = logDisplay;
    }

    public static void Log(string message, LogResult result = LogResult.Info)
    {
        _logDisplay.Log(message, result);
    }
}