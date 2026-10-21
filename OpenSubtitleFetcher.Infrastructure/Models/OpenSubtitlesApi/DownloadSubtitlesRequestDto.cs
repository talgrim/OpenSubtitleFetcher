using System.Text.Json.Serialization;

namespace OpenSubtitleFetcher.Infrastructure.Models.OpenSubtitlesApi;

public class DownloadSubtitlesRequestDto(int fileId, string fileName)
{
    [JsonPropertyName("file_id")]
    public int FileId { get; set; } = fileId;

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = fileName;
}