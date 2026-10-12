using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using OpenSubtitleFetcher.Wpf.Models;

namespace OpenSubtitleFetcher.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private static readonly HashSet<string> VideoFilesExtensions = [".mp4", " .avi", ".mkv"];

    public MainWindow()
    {
        InitializeComponent();
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        SubLanguage.SelectedItem = Models.Language.Polish;
    }

    private void SelectDirectories(object sender, RoutedEventArgs e)
    {
        var openFolderDialog = new OpenFolderDialog
        {
            Multiselect = true,
        };
        
        if (openFolderDialog.ShowDialog() == true)
        {
            Urls.Text = string.Join(';', openFolderDialog.FolderNames);
        }
    }

    private void GetFiles(object sender, RoutedEventArgs e)
    {
        var urls = Urls.Text.Split(';', StringSplitOptions.RemoveEmptyEntries);

        var videoFiles = new ConcurrentBag<VideoFile>();

        Parallel.ForEach(urls, url =>
        {
            var directoryInfo = new DirectoryInfo(url);
            var files = directoryInfo
                .EnumerateFiles()
                .Where(x => VideoFilesExtensions.Contains(x.Extension));

            foreach (var file in files)
            {
                videoFiles.Add(new VideoFile(file.FullName, file.Name));
            }
        });

        FileList.ItemsSource = videoFiles;
    }

    private void Submit(object sender, RoutedEventArgs e)
    {
        var checkedFiles = FileList.ItemsSource.Cast<VideoFile>().Where(x => x.IsChecked);
    }
}