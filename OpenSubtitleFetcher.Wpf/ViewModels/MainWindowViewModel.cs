using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenSubtitleFetcher.Application.UseCases;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Wpf.Helpers.FolderPicker;

namespace OpenSubtitleFetcher.Wpf.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IDownloadSubtitlesUseCase _downloadSubtitlesUseCase;
    private readonly IGetVideoFilesUseCase _getVideoFilesUseCase;
    private readonly IFolderPicker _folderPicker;

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
        var filePaths = FileList.Where(x => x.IsChecked).Select(x => x.FilePath).ToList();

        await Task.Run(() =>
            _downloadSubtitlesUseCase.DownloadSubtitlesForFilesAsync(filePaths, Language, OverwriteSubtitles));
    }
}