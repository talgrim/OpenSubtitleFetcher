using System.Globalization;
using System.Windows.Data;
using OpenSubtitleFetcher.Application.Models.Enums;

namespace OpenSubtitleFetcher.Wpf.Helpers.Converters;

public sealed class StatusToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is FileDownloadStatus status ? StatusIcons.Get(status).Brush : string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}