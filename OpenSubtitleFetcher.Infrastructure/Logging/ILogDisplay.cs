using OpenSubtitleFetcher.Domain.Models.Enums;

namespace OpenSubtitleFetcher.Infrastructure.Logging;

public interface ILogDisplay
{
    void Log(string message, LogResult result = LogResult.Info);
}