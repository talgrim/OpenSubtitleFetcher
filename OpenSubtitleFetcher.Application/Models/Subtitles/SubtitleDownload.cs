namespace OpenSubtitleFetcher.Application.Models.Subtitles;

public record SubtitleDownload(string Link, string Message, int Remaining, string FileName);