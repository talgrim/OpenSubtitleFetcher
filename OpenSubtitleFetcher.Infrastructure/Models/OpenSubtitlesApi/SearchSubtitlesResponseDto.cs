namespace OpenSubtitleFetcher.Infrastructure.Models.OpenSubtitlesApi;

public class SearchSubtitlesResponseDto
{
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int PerPage { get; set; }
    public int Page { get; set; }
    public DataDto[] Data { get; set; } = [];
}