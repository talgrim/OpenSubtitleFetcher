using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenSubtitleFetcher.Application.Models.Enums;
using OpenSubtitleFetcher.Application.Models.Subtitles;
using OpenSubtitleFetcher.Application.UseCases;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Wpf.Helpers.FolderPicker;
using OpenSubtitleFetcher.Wpf.Helpers.Logging;

namespace OpenSubtitleFetcher.Wpf.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IDownloadSubtitlesUseCase _downloadSubtitlesUseCase;
    private readonly IGetVideoFilesUseCase _getVideoFilesUseCase;
    private readonly IFolderPicker _folderPicker;

    [ObservableProperty] public partial Language Language { get; set; } = Language.Polish;
    [ObservableProperty] public partial SubtitleSelectionMode Strategy { get; set; } = SubtitleSelectionMode.BestMatch;

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

    [ObservableProperty] public partial bool OverwriteSubtitles { get; set; } = false;

    public LogViewModel Logs { get; }

    public RelayCommand SelectDirectoriesCommand { get; }
    public RelayCommand GetFilesCommand { get; }
    public AsyncRelayCommand SubmitCommand { get; }

    public MainWindowViewModel(
        IDownloadSubtitlesUseCase downloadSubtitlesUseCase,
        IGetVideoFilesUseCase getVideoFilesUseCase,
        LogViewModel logs,
        IFolderPicker folderPicker)
    {
        _downloadSubtitlesUseCase = downloadSubtitlesUseCase;
        _getVideoFilesUseCase = getVideoFilesUseCase;
        _folderPicker = folderPicker;
        Logs = logs;

        SelectDirectoriesCommand = new RelayCommand(SelectDirectories);
        GetFilesCommand = new RelayCommand(GetFiles, CanGetFiles);
        SubmitCommand = new AsyncRelayCommand(Submit, CanSubmit);
    }

    private void SelectDirectories()
    {
        var folders = _folderPicker.SelectFolders(); 
        if (folders.Length > 0)
        {
            Urls = string.Join(';', folders);
        }
    }

    private bool CanGetFiles()
    {
        return Urls.Length > 0;
    }

    private void GetFiles()
    {
        var urls = Urls.Split(';', StringSplitOptions.RemoveEmptyEntries);

        var files = _getVideoFilesUseCase.GetVideoFiles(urls);

        FileList.Clear();
        foreach (var file in files)
        {
            FileList.Add(new VideoFileViewModel(file.FilePath, file.FileName));
        }

        Logs.Log($"Loaded {FileList.Count} file(s)");

        SubmitCommand.NotifyCanExecuteChanged();
    }

    private bool CanSubmit()
    {
        return FileList.Any(x => x.IsChecked);
    }

    private async Task Submit()
    {
        foreach (var file in FileList)
        {
            file.Status = file.IsChecked ? FileDownloadStatus.Pending : null;
        }

        var rows = FileList.Where(x => x.IsChecked).ToDictionary(x => x.FilePath);

        var progress = new Progress<FileDownloadProgress>(p =>
        {
            rows[p.FilePath].Status = p.Status;
            LogProgress(p);
        });

        await Task.Run(() =>
            _downloadSubtitlesUseCase.DownloadSubtitlesForFilesAsync(
                rows.Keys.ToList(),
                Language,
                Strategy,
                OverwriteSubtitles,
                progress));

        Logs.Log($"Finished processing {rows.Count} file(s)");
    }

    private void LogProgress(FileDownloadProgress file)
    {
        var message = $"Status update for file {Path.GetFileName(file.FilePath)}: {file.Status}";

        if (file.Details is not null)
        {
            message += $" ({file.Details})";
        }

        var result = file.Status switch
        {
            FileDownloadStatus.Downloaded => LogResult.Success,
            FileDownloadStatus.NotProcessed or FileDownloadStatus.NotFound => LogResult.Warning,
            FileDownloadStatus.Failed => LogResult.Error,
            _ => LogResult.Info
        };

        Logs.Log(message, result);
    }
}