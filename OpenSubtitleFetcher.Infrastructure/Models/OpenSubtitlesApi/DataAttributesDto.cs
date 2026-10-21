namespace OpenSubtitleFetcher.Infrastructure.Models.OpenSubtitlesApi;

public class DataAttributesDto
{
    public string Language { get; set; }
    public int DownloadCount { get; set; }
    public bool Hd { get; set; }
    public string Fps { get; set; }
    public bool FromTrusted { get; set; }
    public string Url { get; set; }
    public FileDto[] Files { get; set; } = [];
}