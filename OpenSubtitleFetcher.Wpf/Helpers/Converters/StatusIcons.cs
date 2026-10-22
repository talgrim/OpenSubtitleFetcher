using System.Windows.Media;
using OpenSubtitleFetcher.Application.Models.Enums;

namespace OpenSubtitleFetcher.Wpf.Helpers.Converters;

internal static class StatusIcons
{
    public static (string Glyph, Brush Brush) Get(FileDownloadStatus status) => status switch
    {
        FileDownloadStatus.Pending => ("\uE823", Brushes.Gray), // Clock
        FileDownloadStatus.Skipped => ("\uE72A", Brushes.Gray), // Forward
        FileDownloadStatus.Searching => ("\uE721", Brushes.SteelBlue), // Search
        FileDownloadStatus.Downloading => ("\uE896", Brushes.SteelBlue), // Download
        FileDownloadStatus.Downloaded => ("\uE73E", Brushes.ForestGreen), // CheckMark
        FileDownloadStatus.NotFound => ("\uE7BA", Brushes.DarkOrange), // Warning
        FileDownloadStatus.NotProcessed => ("\uE733", Brushes.DarkOrange), // Blocked
        FileDownloadStatus.Failed => ("\uE711", Brushes.Firebrick), // Cancel
        _ => (string.Empty, Brushes.Transparent)
    };
}