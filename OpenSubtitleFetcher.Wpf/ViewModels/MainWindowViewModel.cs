using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OpenSubtitleFetcher.Application.UseCases;
using OpenSubtitleFetcher.Domain.Models.Enums;

namespace OpenSubtitleFetcher.Wpf.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IDownloadSubtitlesUseCase _downloadSubtitlesUseCase;
    private readonly IGetVideoFilesUseCase _getVideoFilesUseCase;

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
        LogViewModel logs)
    {
        _downloadSubtitlesUseCase = downloadSubtitlesUseCase;
        _getVideoFilesUseCase = getVideoFilesUseCase;
        Logs = logs;

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