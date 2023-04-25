using NexusMods.Archives.Nx.Enums;

namespace NexusMods.Archives.Nx.Structs;

/// <summary>
///     Controls the behaviour of the packer.
/// </summary>
public class PackerSettings
{
    /// <summary>
    ///     Reports progress back to the process.
    /// </summary>
    public IProgress<double>? Progress { get; set; }

    /// <summary>
    ///     The path the resulting file is output to.
    /// </summary>
    public required string OutputPath { get; set; }

    /// <summary>
    ///     Maximum number of threads allowed.
    /// </summary>
    public int MaxNumThreads { get; set; }

    /// <summary>
    ///     Size of SOLID blocks.
    /// </summary>
    public int BlockSize { get; set; }

    /// <summary>
    ///     Size of large file SOLID chunks.
    /// </summary>
    public int ChunkSize { get; set; }

    /// <summary>
    ///     Compression level to use for ZStandard if ZStandard is used.
    /// </summary>
    public int ZStandardLevel { get; set; } = 16;

    /// <summary>
    ///     Compression level to use for LZ4 if LZ4 is used.
    /// </summary>
    public int Lz4Level { get; set; } = 12;

    /// <summary>
    ///     Compression algorithm used for compressing SOLID blocks.
    /// </summary>
    public CompressionPreference SolidBlockAlgorithm { get; set; } = CompressionPreference.Lz4;

    /// <summary>
    ///     Compression algorithm used for compressing chunked files.
    /// </summary>
    public CompressionPreference ChunkedFileAlgorithm { get; set; } = CompressionPreference.ZStandard;
}
