using OpenSubtitleFetcher.Infrastructure.Repositories;

namespace OpenSubtitleFetcher.Infrastructure.Tests.Repositories;

public sealed class VideoFileRepositoryTests : IDisposable
{
    private readonly TempDirectory _directory = new();
    private readonly VideoFileRepository _repository = new();

    [Fact]
    public void GetVideoFiles_ReturnsOnlyVideoFiles()
    {
        // Arrange
        var mkv = _directory.CreateFile("A.mkv");
        var mp4 = _directory.CreateFile("B.mp4");
        var avi = _directory.CreateFile("C.avi");
        _directory.CreateFile("A.srt");
        _directory.CreateFile("notes.txt");

        // Act
        var result = _repository.GetVideoFiles([_directory.Path]);

        // Assert
        Assert.Equal(new[] { mkv, mp4, avi }.Order(), result.Select(x => x.FilePath).Order());
    }

    [Fact]
    public void GetVideoFiles_MatchesExtensionsCaseInsensitively()
    {
        // Arrange
        var upper = _directory.CreateFile("Film.MKV");

        // Act
        var result = _repository.GetVideoFiles([_directory.Path]);

        // Assert
        Assert.Equal(upper, Assert.Single(result).FilePath);
    }

    [Fact]
    public void GetVideoFiles_FillsFileName()
    {
        // Arrange
        _directory.CreateFile("Film.mkv");

        // Act
        var result = _repository.GetVideoFiles([_directory.Path]);

        // Assert
        Assert.Equal("Film.mkv", Assert.Single(result).FileName);
    }

    [Fact]
    public void GetVideoFiles_CombinesMultipleDirectories()
    {
        // Arrange
        var first = _directory.CreateFile(Path.Combine("First", "A.mkv"));
        var second = _directory.CreateFile(Path.Combine("Second", "B.mkv"));

        // Act
        var result = _repository.GetVideoFiles(
            [Path.Combine(_directory.Path, "First"), Path.Combine(_directory.Path, "Second")]);

        // Assert
        Assert.Equal(new[] { first, second }.Order(), result.Select(x => x.FilePath).Order());
    }

    [Fact]
    public void GetVideoFiles_DoesNotSearchSubdirectories()
    {
        // Arrange
        _directory.CreateFile(Path.Combine("Nested", "Film.mkv"));

        // Act
        var result = _repository.GetVideoFiles([_directory.Path]);

        // Assert
        Assert.Empty(result);
    }

    public void Dispose()
    {
        _directory.Dispose();
    }
}
