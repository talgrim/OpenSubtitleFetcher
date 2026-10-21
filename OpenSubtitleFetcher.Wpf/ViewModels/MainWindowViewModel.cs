using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OpenSubtitleFetcher.Application;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Infrastructure.Logging;
using OpenSubtitleFetcher.Wpf.Helpers.UiContexts;

namespace OpenSubtitleFetcher.Wpf.ViewModels;

public partial class MainWindowViewModel : ObservableObject, ILogDisplay
{
    private static readonly HashSet<string> VideoFilesExtensions = [".mp4", " .avi", ".mkv"];
    private readonly ITemp _temp;
    private readonly StringBuilder _stringBuilder = new();

    [ObservableProperty] public partial Language Language { get; set; } = Language.Polish;

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

    [ObservableProperty] public partial string LogBlock { get; set; } = string.Empty;

    public RelayCommand SelectDirectoriesCommand { get; }
    public RelayCommand GetFilesCommand { get; }
    public AsyncRelayCommand SubmitCommand { get; }

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

        var bag = new ConcurrentBag<VideoFileViewModel>();

        Parallel.ForEach(urls, url =>
        {
            var directoryInfo = new DirectoryInfo(url);
            var files = directoryInfo
                .EnumerateFiles()
                .Where(x => VideoFilesExtensions.Contains(x.Extension));

            foreach (var file in files)
            {
                bag.Add(new VideoFileViewModel(file.FullName, file.Name));
            }
        });

        UiContext.Invoke(() =>
        {
            FileList.Clear();
            foreach (var file in bag)
                FileList.Add(file);
        });

        Log($"Loaded {FileList.Count} file(s)");

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

    public void Log(string message, LogResult logLevel = LogResult.Info)
    {
        _stringBuilder.AppendLine($"{DateTime.Now}: [{logLevel}] - {message}");
        UiContext.Invoke(() => LogBlock = _stringBuilder.ToString());
    }
}