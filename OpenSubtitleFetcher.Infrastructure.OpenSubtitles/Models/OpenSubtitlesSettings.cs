namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Models;

public class OpenSubtitlesSettings
{
    public required string ApiKey { get; set; }
    public required string Url { get; set; }
    public required int Version { get; set; }
    public required string UserAgent { get; set; }
}