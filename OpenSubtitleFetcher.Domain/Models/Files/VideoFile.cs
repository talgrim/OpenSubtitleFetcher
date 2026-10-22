namespace OpenSubtitleFetcher.Domain.Models.Files;

public record VideoFile(string FilePath, string FileName)
{
    public static readonly HashSet<string> Extensions = [".mp4", ".avi", ".mkv"];
}