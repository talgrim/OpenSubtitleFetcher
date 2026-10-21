using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OpenSubtitleFetcher.Application;
using OpenSubtitleFetcher.Domain.Models.Enums;

namespace OpenSubtitleFetcher.Wpf.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private static readonly HashSet<string> VideoFilesExtensions = [".mp4", " .avi", ".mkv"];
    private readonly ITemp _temp;

    [ObservableProperty]
    public partial Language Language { get; set; } = Language.Polish;

    public string Urls
    {
        get;
        set
        {
            SetProperty(ref field, value);
            GetFilesCommand.NotifyCanExecuteChanged();
        }
    } = string.Empty;

    public ObservableCollection<VideoFileViewModel> FileList { get; set; } = [];

    public RelayCommand SelectDirectoriesCommand { get; private set; }
    public RelayCommand GetFilesCommand { get; private set; }
    public AsyncRelayCommand SubmitCommand { get; private set; }

    public MainWindowViewModel(ITemp temp)
    {
        _temp = temp;

        SelectDirectoriesCommand = new RelayCommand(SelectDirectories);
        GetFilesCommand = new RelayCommand(GetFiles, CanGetFiles);
        SubmitCommand = new AsyncRelayCommand(Submit, CanSubmit);
    }

    private void SelectDirectories()
    {
        var openFolderDialog = new OpenFolderDialog
        {
            Multiselect = true,
        };
        
        if (openFolderDialog.ShowDialog() == true)
        {
            Urls = string.Join(';', openFolderDialog.FolderNames);
        }
    }

    private bool CanGetFiles()
    {
        return Urls.Length > 0;
    }

    private void GetFiles()
    {
        var urls = Urls.Split(';', StringSplitOptions.RemoveEmptyEntries);

        FileList.Clear();

        Parallel.ForEach(urls, url =>
        {
            var directoryInfo = new DirectoryInfo(url);
            var files = directoryInfo
                .EnumerateFiles()
                .Where(x => VideoFilesExtensions.Contains(x.Extension));

            foreach (var file in files)
            {
                FileList.Add(new VideoFileViewModel(file.FullName, file.Name));
            }
        });

        SubmitCommand.NotifyCanExecuteChanged();
    }

    private bool CanSubmit()
    {
        return FileList.Any(x => x.IsChecked);
    }

    private async Task Submit()
    {
        var checkedFiles = FileList.Where(x => x.IsChecked).ToList();

        var query = checkedFiles.First().FileName;
        _ = Task.Run(() => _temp.TestAsync(query, Language));
    }
}