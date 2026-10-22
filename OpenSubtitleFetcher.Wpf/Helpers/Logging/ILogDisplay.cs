namespace OpenSubtitleFetcher.Wpf.Helpers.Logging;

public interface ILogDisplay
{
    void Log(string message, LogResult result = LogResult.Info);
}