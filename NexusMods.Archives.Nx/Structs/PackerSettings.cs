using JetBrains.Annotations;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Structs;

/// <summary>
///     Controls the behaviour of the packer.
/// </summary>
/// <remarks>
///     This class is not thread safe and stores mutable state tied to the packer.
///     For example, the progress.
///
///     Please clone settings for each packer instance.
/// </remarks>
[PublicAPI]
public class PackerSettings
{
    /// <summary>
    ///     Reports progress back to the process.
    /// </summary>
    public IProgress<double>? Progress { get; set; }

    /// <summary>
    ///     If not null, chunked files are deduplicated, otherwise set this to null.
    ///     Chunked deduplication encurs a small amount of overhead for each file.
    /// </summary>
    public ChunkedDeduplicationState? ChunkedDeduplicationState { get; set; }

    /// <summary>
    ///     If not null, files are deduplicated, otherwise set this to null.
    ///     Solid deduplication encurs a small amount of overhead for each block.
    /// </summary>
    public SolidDeduplicationState? SolidDeduplicationState { get; set; } = new();

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
    public int MaxNumThreads { get; set; } = NxEnvironment.PhysicalCoreCount;
    // HT/SMT hurts here, due to sub-par concurrency handling and fact core cache is split.

    /// <summary>
    ///     Size of SOLID blocks.
    ///     Range is 4095 to 67108863 (64 MiB).
    ///     Must be smaller than <see cref="ChunkSize" />.
    /// </summary>
    public int BlockSize { get; set; } = 1048575;

    /// <summary>
    ///     Size of large file chunks.
    ///     Range is 32768 (32K) to 1073741824 (1 GiB).
    /// </summary>
    public int ChunkSize { get; set; } = 1048576;

    /// <summary>
    ///     Compression level to use for SOLID data.
    ///     ZStandard has Range -5 - 22.<br />
    ///     LZ4 has Range: 1 - 12.<br />
    /// </summary>
    public int SolidCompressionLevel { get; set; } = 16;

    /// <summary>
    ///     Compression level to use for chunked data.
    ///     ZStandard has Range -5 - 22.<br />
    ///     LZ4 has Range: 1 - 12.<br />
    /// </summary>
    public int ChunkedCompressionLevel { get; set; } = 9;

    /// <summary>
    ///     Compression algorithm used for compressing SOLID blocks.
    /// </summary>
    public CompressionPreference SolidBlockAlgorithm { get; set; } = CompressionPreference.ZStandard;

    /// <summary>
    ///     Compression algorithm used for compressing chunked files.
    /// </summary>
    public CompressionPreference ChunkedFileAlgorithm { get; set; } = CompressionPreference.ZStandard;

    /// <summary>
    ///     Sanitizes settings to acceptable values if they are out of range or undefined.
    /// </summary>
    public void Sanitize()
    {
        ChunkedDeduplicationState?.Reset();
        SolidDeduplicationState?.Reset();
        if (SolidBlockAlgorithm == CompressionPreference.NoPreference || !CompressionPreferenceExtensions.IsDefined(SolidBlockAlgorithm))
            SolidBlockAlgorithm = CompressionPreference.ZStandard;

        if (ChunkedFileAlgorithm == CompressionPreference.NoPreference || !CompressionPreferenceExtensions.IsDefined(ChunkedFileAlgorithm))
            ChunkedFileAlgorithm = CompressionPreference.ZStandard;

        // Note: BlockSize is minus one, see spec.
        if (BlockSize < 0) // prevent underflow on min value
            BlockSize = 0;

        BlockSize = Polyfills.RoundUpToPowerOf2NoOverflow(BlockSize) - 1;
        ChunkSize = Polyfills.RoundUpToPowerOf2NoOverflow(ChunkSize);

        BlockSize = Polyfills.Clamp(BlockSize, 4095, 67108863);
        ChunkSize = Polyfills.Clamp(ChunkSize, 32768, 1073741824); // 1GiB because we can't pool more with standard ArrayPool.
        if (ChunkSize <= BlockSize)
            ChunkSize = BlockSize + 1;

        SolidCompressionLevel = ClampCompression(SolidCompressionLevel, SolidBlockAlgorithm);
        ChunkedCompressionLevel = ClampCompression(ChunkedCompressionLevel, ChunkedFileAlgorithm);
        MaxNumThreads = Polyfills.Clamp(MaxNumThreads, 1, int.MaxValue);
    }

    /// <summary>
    ///     Retrieves the compression level for the specified algorithm.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Compression algorithm used is unsupported.</exception>
    // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
    private int ClampCompression(int level, CompressionPreference preference) => preference switch
    {
        CompressionPreference.Copy => 0,
        CompressionPreference.ZStandard => Polyfills.Clamp(level, -5, 22),
        CompressionPreference.Lz4 => Polyfills.Clamp(level, 1, 12),
        _ => throw new ArgumentOutOfRangeException(nameof(preference), preference, null)
    };
}
