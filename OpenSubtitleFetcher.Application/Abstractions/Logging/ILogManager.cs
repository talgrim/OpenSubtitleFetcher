using OpenSubtitleFetcher.Application.Models.Enums;

namespace OpenSubtitleFetcher.Application.Abstractions.Logging;

public interface ILogManager
{
    void Initialize(ILogDisplay logDisplay);
    void Log(string message, LogResult result = LogResult.Info);
}