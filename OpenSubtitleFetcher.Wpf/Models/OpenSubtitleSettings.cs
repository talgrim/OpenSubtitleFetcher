namespace OpenSubtitleFetcher.Wpf.Models;

public class OpenSubtitleSettings
{
    public const string SectionName = "OpenSubtitleConfig";
    public required string ApiKey { get; set; }
}