using System.Buffers.Binary;

namespace OpenSubtitleFetcher.Infrastructure.OpenSubtitles.Hashing;

internal static class MovieHash
{
    private const int ChunkSize = 64 * 1024;

    public static async Task<string?> ComputeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1,
            useAsync: true);

        if (stream.Length < 2 * ChunkSize)
        {
            return null;
        }

        var buffer = new byte[ChunkSize];
        var hash = (ulong)stream.Length;

        await stream.ReadExactlyAsync(buffer, cancellationToken);
        hash = unchecked(hash + Sum(buffer));

        stream.Seek(-ChunkSize, SeekOrigin.End);
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        hash = unchecked(hash + Sum(buffer));

        return hash.ToString("x16");
    }

    private static ulong Sum(ReadOnlySpan<byte> chunk)
    {
        ulong sum = 0;

        for (var i = 0; i < chunk.Length; i += sizeof(ulong))
        {
            sum = unchecked(sum + BinaryPrimitives.ReadUInt64LittleEndian(chunk[i..]));
        }

        return sum;
    }
}