namespace OpenSubtitleFetcher.Domain.Models.Subtitles;

public class SubtitleFormat
{
    public const string DefaultExtension = ".srt";
    public static readonly string[] SourceExtensions = [".srt", ".sub", ".ass", ".ssa", ".vtt"];
}