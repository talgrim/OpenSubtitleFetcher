namespace OpenSubtitleFetcher.Application.Models.Enums;

public enum FileDownloadStatus
{
    Pending,
    Skipped,
    Searching,
    NotFound,
    Downloading,
    Downloaded,
    Failed,
    NotProcessed
}