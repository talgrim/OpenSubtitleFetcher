namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Models.OpenSubtitlesApi;

internal class DownloadSubtitlesRequestDto(int fileId, string fileName)
{
    public int FileId { get; set; } = fileId;
    public string FileName { get; set; } = fileName;
}