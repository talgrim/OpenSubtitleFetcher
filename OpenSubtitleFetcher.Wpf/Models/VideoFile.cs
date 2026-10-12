using System.Diagnostics;

namespace OpenSubtitleFetcher.Wpf.Models;

[DebuggerDisplay("{FileName}")]
public class VideoFile(string filePath, string fileName)
{
    public bool IsChecked { get; set; } = true;
    public string FileName { get; set; } = fileName;
    public string FilePath { get; set; } = filePath;
}