using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using OpenSubtitleFetcher.Application;
using OpenSubtitleFetcher.Wpf.Models;
using DomainLanguage = OpenSubtitleFetcher.Domain.Models.Enums.Language;

namespace OpenSubtitleFetcher.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private readonly ITemp _temp;
    private static readonly HashSet<string> VideoFilesExtensions = [".mp4", " .avi", ".mkv"];

    public MainWindow(ITemp temp)
    {
        _temp = temp;
        InitializeComponent();
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        SubLanguage.SelectedItem = DomainLanguage.Polish;
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

    private async void Submit(object sender, RoutedEventArgs e)
    {
        var checkedFiles = FileList.ItemsSource?.Cast<VideoFile>().Where(x => x.IsChecked).ToList();

        if (checkedFiles is null || checkedFiles.Count == 0)
        {
            return;
        }

        var query = checkedFiles.First()!.FileName;
        var language = (DomainLanguage)SubLanguage.SelectedItem;
        await _temp.TestAsync(query, language);
    }
}