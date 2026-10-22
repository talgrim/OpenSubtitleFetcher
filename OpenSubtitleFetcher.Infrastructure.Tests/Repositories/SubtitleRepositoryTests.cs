using OpenSubtitleFetcher.Infrastructure.Repositories;

namespace OpenSubtitleFetcher.Infrastructure.Tests.Repositories;

public sealed class SubtitleRepositoryTests : IDisposable
{
    private readonly TempDirectory _directory = new();
    private readonly SubtitleRepository _repository = new();

    [Theory]
    [InlineData(".srt")]
    [InlineData(".sub")]
    [InlineData(".ass")]
    [InlineData(".ssa")]
    [InlineData(".vtt")]
    public void HasSubtitles_ReturnsTrue_WhenSubtitlesFileExists(string extension)
    {
        // Arrange
        var videoPath = _directory.CreateFile("Film.mkv");
        _directory.CreateFile("Film" + extension);

        // Act
        var result = _repository.HasSubtitles(videoPath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasSubtitles_ReturnsFalse_WhenOnlyOtherFilesExist()
    {
        // Arrange
        var videoPath = _directory.CreateFile("Film.mkv");
        _directory.CreateFile("Film.txt");
        _directory.CreateFile("Other.srt");

        // Act
        var result = _repository.HasSubtitles(videoPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SaveAsync_WritesSrtNextToVideo_AndLeavesVideoUntouched()
    {
        // Arrange
        var videoPath = _directory.CreateFile("Film.mkv", "video");
        var subtitlesPath = Path.Combine(_directory.Path, "Film.srt");
        byte[] content = [1, 2, 3];

        // Act
        await _repository.SaveAsync(videoPath, content, CancellationToken.None);

        // Assert
        Assert.Equal(content, await File.ReadAllBytesAsync(subtitlesPath));
        Assert.Equal("video", await File.ReadAllTextAsync(videoPath));
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingSubtitles()
    {
        // Arrange
        var videoPath = _directory.CreateFile("Film.mkv");
        var subtitlesPath = _directory.CreateFile("Film.srt", "old");

        // Act
        await _repository.SaveAsync(videoPath, [42], CancellationToken.None);

        // Assert
        Assert.Equal([42], await File.ReadAllBytesAsync(subtitlesPath));
    }

    public void Dispose()
    {
        _directory.Dispose();
    }
}
