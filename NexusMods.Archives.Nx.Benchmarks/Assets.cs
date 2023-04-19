using static SharpZstd.Interop.Zstd;

namespace NexusMods.Archives.Nx.Benchmarks;

public class Assets
{
    /// <summary>
    ///     All files from Yakuza Kiwami game folder (~4000 files).
    /// </summary>
    public static string[] GetYakuzaFileList()
    {
        var compressed = File.ReadAllBytes("Assets/FileLists/YakuzaKiwami.zst");
        return AsLines(DecompressZStd(compressed)).ToArray();
    }

    /// <summary>
    ///     Decompresses a ZStd compressed buffer.
    /// </summary>
    /// <param name="srcData">The data to decompress.</param>
    public static unsafe byte[] DecompressZStd(byte[] srcData)
    {
        fixed (byte* compressedPtr = srcData)
        {
            var decompSize = ZSTD_findDecompressedSize(compressedPtr, (UIntPtr)srcData.Length);
            var decompressedBuf = new byte[decompSize];
            fixed (byte* decompressedPtr = decompressedBuf)
            {
                var decompressed = (int)ZSTD_decompress(decompressedPtr, (UIntPtr)decompressedBuf.Length, compressedPtr,
                    (UIntPtr)srcData.Length);
                return decompressedBuf[..decompressed].ToArray();
            }
        }
    }

    /// <summary>
    ///     Converts byte array to collection of lines.
    /// </summary>
    /// <param name="srcData">The data to decompress.</param>
    public static IEnumerable<string> AsLines(byte[] srcData)
    {
        using var memStream = new MemoryStream(srcData);
        using var streamReader = new StreamReader(memStream);
        string? line;
        while ((line = streamReader.ReadLine()) != null) yield return line;
    }
}
