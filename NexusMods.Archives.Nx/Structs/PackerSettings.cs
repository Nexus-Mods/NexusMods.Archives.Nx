using JetBrains.Annotations;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Structs;

/// <summary>
///     Controls the behaviour of the packer.
/// </summary>
[PublicAPI]
public class PackerSettings
{
    /// <summary>
    ///     Reports progress back to the process.
    /// </summary>
    public IProgress<double>? Progress { get; set; }

    /// <summary>
    ///     The stream to which data is output to.
    ///     This stream must support seeking.
    /// </summary>
    /// <remarks>
    ///     This assumes the stream starts at offset 0.
    ///     If you need the ability to write to a middle of an existing stream; raise a PR.
    /// </remarks>
    public required Stream Output { get; set; }

    /// <summary>
    ///     Maximum number of threads allowed.
    /// </summary>
    public int MaxNumThreads { get; set; } = Environment.ProcessorCount;

    /// <summary>
    ///     Size of SOLID blocks.
    ///     Range is 32767 to 67108863 (64 MiB).
    ///     Must be smaller than <see cref="ChunkSize"/>.
    /// </summary>
    public int BlockSize { get; set; } = 1048575;

    /// <summary>
    ///     Size of large file chunks.
    ///     Range is 4194304 (4 MiB) to 536870912 (512 MiB).
    /// </summary>
    public int ChunkSize { get; set; } = 16777216;

    /// <summary>
    ///     Compression level to use for ZStandard if ZStandard is used.
    ///     Range: 1 - 22.
    /// </summary>
    public int ZStandardLevel { get; set; } = 16;

    /// <summary>
    ///     Compression level to use for LZ4 if LZ4 is used.
    ///     Range: 1 - 12.
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

    /// <summary>
    ///     Sanitizes settings to acceptable values if they are out of range or undefined.
    /// </summary>
    public void Sanitize()
    {
        if (SolidBlockAlgorithm == CompressionPreference.NoPreference || !CompressionPreferenceExtensions.IsDefined(SolidBlockAlgorithm))
            SolidBlockAlgorithm = CompressionPreference.Lz4;

        if (ChunkedFileAlgorithm == CompressionPreference.NoPreference || !CompressionPreferenceExtensions.IsDefined(ChunkedFileAlgorithm))
            ChunkedFileAlgorithm = CompressionPreference.ZStandard;

        // Note: BlockSize is minus one, see spec.
        if (BlockSize < 0) // prevent underflow on min value
            BlockSize = 0;

        BlockSize = Polyfills.RoundUpToPowerOf2NoOverflow(BlockSize) - 1;
        ChunkSize = Polyfills.RoundUpToPowerOf2NoOverflow(ChunkSize);

        BlockSize = Polyfills.Clamp(BlockSize, 32767, 67108863);
        ChunkSize = Polyfills.Clamp(ChunkSize, 4194304, 536870912);
        if (ChunkSize <= BlockSize)
            ChunkSize = BlockSize + 1;

        ZStandardLevel = Polyfills.Clamp(ZStandardLevel, 1, 22);
        Lz4Level = Polyfills.Clamp(Lz4Level, 1, 12);
        MaxNumThreads = Polyfills.Clamp(MaxNumThreads, 1, int.MaxValue);
    }

    /// <summary>
    ///     Retrieves the compression level for the specified algorithm.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Compression algorithm used is unsupported.</exception>
    // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
    public int GetCompressionLevel(CompressionPreference preference) => preference switch
    {
        CompressionPreference.Copy => 0,
        CompressionPreference.ZStandard => ZStandardLevel,
        CompressionPreference.Lz4 => Lz4Level,
        _ => throw new ArgumentOutOfRangeException(nameof(preference), preference, null)
    };
}
