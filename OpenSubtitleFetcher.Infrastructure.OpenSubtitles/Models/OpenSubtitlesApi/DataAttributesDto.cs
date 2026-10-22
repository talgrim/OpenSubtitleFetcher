namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Models.OpenSubtitlesApi;

internal class DataAttributesDto
{
    public string Language { get; set; }
    public int DownloadCount { get; set; }
    public bool? Hd { get; set; }
    public double? Fps { get; set; }
    public bool? FromTrusted { get; set; }
    public bool? MachineTranslated { get; set; }
    public string Url { get; set; }
    public FileDto[] Files { get; set; } = [];

    // ReSharper disable once IdentifierTypo - name is compatible with OpenSubtitles
    public bool? MoviehashMatch { get; set; }
}