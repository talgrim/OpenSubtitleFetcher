using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenSubtitleFetcher.Wpf.ViewModels;

[DebuggerDisplay("{FileName}")]
public partial class VideoFileViewModel(string filePath, string fileName) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsChecked { get; set; } = true;
    [ObservableProperty]
    public partial string FileName { get; set; } = fileName;
    [ObservableProperty]
    public partial string FilePath { get; set; } = filePath;
}