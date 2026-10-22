using OpenSubtitleFetcher.Application.Models.Enums;

namespace OpenSubtitleFetcher.Application.Models.Subtitles;

public record FileDownloadProgress(string FilePath, FileDownloadStatus Status, string? Details = null);