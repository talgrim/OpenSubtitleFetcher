using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using OpenSubtitleFetcher.Application.Abstractions.Logging;
using OpenSubtitleFetcher.Application.Abstractions.Repositories;
using OpenSubtitleFetcher.Application.Models.Enums;
using OpenSubtitleFetcher.Application.UseCases;
using OpenSubtitleFetcher.Domain.Models.Enums;
using OpenSubtitleFetcher.Wpf.Helpers.UiContexts;

namespace OpenSubtitleFetcher.Wpf.ViewModels;

public partial class MainWindowViewModel : ObservableObject, ILogDisplay
{
    private readonly IDownloadSubtitlesUseCase _downloadSubtitlesUseCase;
    private readonly IVideoFileRepository _videoFileRepository;
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
    [ObservableProperty] public partial bool OverwriteSubtitles { get; set; } = false;

    public RelayCommand SelectDirectoriesCommand { get; }
    public RelayCommand GetFilesCommand { get; }
    public AsyncRelayCommand SubmitCommand { get; }

    public MainWindowViewModel(
        IDownloadSubtitlesUseCase downloadSubtitlesUseCase,
        IVideoFileRepository videoFileRepository)
    {
        _downloadSubtitlesUseCase = downloadSubtitlesUseCase;
        _videoFileRepository = videoFileRepository;

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

        var files = _videoFileRepository.GetVideoFiles(urls);

        FileList.Clear();
        foreach (var file in files)
        {
            FileList.Add(new VideoFileViewModel(file.FilePath, file.FileName));
        }

        Log($"Loaded {FileList.Count} file(s)");

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

    public void Log(string message, LogResult logLevel = LogResult.Info)
    {
        string log;

        // Log is called from parallel downloads
        lock (_stringBuilder)
        {
            _stringBuilder.AppendLine($"{DateTime.Now}: [{logLevel}] - {message}");
            log = _stringBuilder.ToString();
        }

        UiContext.Invoke(() => LogBlock = log);
    }
}