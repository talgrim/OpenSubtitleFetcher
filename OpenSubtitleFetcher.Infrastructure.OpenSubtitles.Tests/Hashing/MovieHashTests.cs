using OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Hashing;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Tests.Hashing;

public sealed class MovieHashTests : IDisposable
{
    private const int ChunkSize = 64 * 1024;
    private const int FileSize = 200_000;

    private readonly TempFiles _files = new();

    [Fact]
    public async Task ComputeAsync_ReturnsFileSize_ForFileOfZeros()
    {
        // Arrange: zero bytes add nothing, so the hash is just the size
        var path = _files.Create("zeros.mkv", new byte[FileSize]);

        // Act
        var hash = await MovieHash.ComputeAsync(path);

        // Assert
        Assert.Equal(Hex(FileSize), hash);
    }

    [Fact]
    public async Task ComputeAsync_AddsWordsFromFirstAndLastChunk()
    {
        // Arrange
        var content = new byte[FileSize];
        content[0] = 1;              // first word of the first chunk
        content[ChunkSize - 8] = 2;  // last word of the first chunk
        content[^ChunkSize] = 3;     // first word of the last chunk
        content[^8] = 4;             // last word of the last chunk
        var path = _files.Create("film.mkv", content);

        // Act
        var hash = await MovieHash.ComputeAsync(path);

        // Assert
        Assert.Equal(Hex(FileSize + 1 + 2 + 3 + 4), hash);
    }

    [Fact]
    public async Task ComputeAsync_IgnoresMiddleOfFile()
    {
        // Arrange
        var content = new byte[FileSize];
        content.AsSpan(ChunkSize, FileSize - 2 * ChunkSize).Fill(0xAB);
        var path = _files.Create("film.mkv", content);

        // Act
        var hash = await MovieHash.ComputeAsync(path);

        // Assert
        Assert.Equal(Hex(FileSize), hash);
    }

    [Fact]
    public async Task ComputeAsync_ReadsWordsAsLittleEndian()
    {
        // Arrange: the second byte of a little-endian word is worth 256
        var content = new byte[FileSize];
        content[1] = 1;
        var path = _files.Create("film.mkv", content);

        // Act
        var hash = await MovieHash.ComputeAsync(path);

        // Assert
        Assert.Equal(Hex(FileSize + 256), hash);
    }

    [Fact]
    public async Task ComputeAsync_WrapsAroundOnOverflow()
    {
        // Arrange: each of the 2 × 8192 words is ulong.MaxValue, i.e. -1 modulo 2^64
        var content = Enumerable.Repeat((byte)0xFF, FileSize).ToArray();
        var path = _files.Create("film.mkv", content);

        // Act
        var hash = await MovieHash.ComputeAsync(path);

        // Assert
        Assert.Equal(Hex(FileSize - 2 * (ChunkSize / 8)), hash);
    }

    [Fact]
    public async Task ComputeAsync_HashesFileOfExactlyTwoChunks()
    {
        // Arrange
        var path = _files.Create("film.mkv", new byte[2 * ChunkSize]);

        // Act
        var hash = await MovieHash.ComputeAsync(path);

        // Assert
        Assert.Equal(Hex(2 * ChunkSize), hash);
    }

    [Fact]
    public async Task ComputeAsync_ReturnsNull_ForFileSmallerThanTwoChunks()
    {
        // Arrange
        var path = _files.Create("small.mkv", new byte[2 * ChunkSize - 1]);

        // Act
        var hash = await MovieHash.ComputeAsync(path);

        // Assert
        Assert.Null(hash);
    }

    [Fact]
    public async Task ComputeAsync_Throws_WhenFileDoesNotExist()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.mkv");

        // Act
        var act = () => MovieHash.ComputeAsync(path);

        // Assert
        await Assert.ThrowsAnyAsync<IOException>(act);
    }

    public void Dispose()
    {
        _files.Dispose();
    }

    private static string Hex(long value)
    {
        return ((ulong)value).ToString("x16");
    }
}
