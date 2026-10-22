using Moq;
using OpenSubtitleFetcher.Application.Abstractions.Repositories;
using OpenSubtitleFetcher.Application.UseCases;
using OpenSubtitleFetcher.Domain.Models.Files;

namespace OpenSubtitleFetcher.Application.Tests.UseCases;

public class GetVideoFilesUseCaseTests
{
    [Fact]
    public void GetVideoFiles_ReturnsFilesFromRepository()
    {
        // Arrange
        string[] paths = [@"C:\Movies", @"D:\Series"];
        IReadOnlyCollection<VideoFile> files = [new(@"C:\Movies\Film.mkv", "Film.mkv")];

        var repository = new Mock<IVideoFileRepository>();
        repository.Setup(x => x.GetVideoFiles(paths)).Returns(files);

        var useCase = new GetVideoFilesUseCase(repository.Object);

        // Act
        var result = useCase.GetVideoFiles(paths);

        // Assert
        Assert.Equal(files, result);
    }

    [Fact]
    public void GetVideoFiles_RemovesDuplicates_WhenSameFolderIsSelectedTwice()
    {
        // Arrange
        string[] paths = [@"C:\Movies", @"C:\Movies"];
        VideoFile film = new(@"C:\Movies\Film.mkv", "Film.mkv");
        VideoFile other = new(@"C:\Movies\Other.mkv", "Other.mkv");

        var repository = new Mock<IVideoFileRepository>();
        repository.Setup(x => x.GetVideoFiles(paths)).Returns([film, other, film, other]);

        var useCase = new GetVideoFilesUseCase(repository.Object);

        // Act
        var result = useCase.GetVideoFiles(paths);

        // Assert
        Assert.Equal([film, other], result);
    }
}
