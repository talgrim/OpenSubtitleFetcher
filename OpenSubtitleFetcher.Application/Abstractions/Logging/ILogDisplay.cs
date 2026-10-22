using OpenSubtitleFetcher.Application.Models.Enums;

namespace OpenSubtitleFetcher.Application.Abstractions.Logging;

public interface ILogDisplay
{
    void Log(string message, LogResult result = LogResult.Info);
}