namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Tests;

internal sealed class TempFiles : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public TempFiles()
    {
        Directory.CreateDirectory(_directory);
    }

    public string Create(string fileName, byte[] content)
    {
        var path = Path.Combine(_directory, fileName);
        File.WriteAllBytes(path, content);

        return path;
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }
}
