using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers.Enums;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Headers.Structs;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Headers;

/// <summary>
///     Class with all of the logic responsible for building the table of contents during a packing operations.
/// </summary>
internal class TableOfContentsBuilder<T> : IDisposable where T : IHasRelativePath, IHasFileSize, ICanProvideFileData
{
    /// <summary>
    ///     The underlying table of contents.
    /// </summary>
    public TableOfContents Toc = null!;

    /// <summary>
    ///     Dictionary that maps file names to their corresponding file indexes in pool.
    /// </summary>
    /// <remarks>
    ///     The purpose of this dictionary lies in parallel packing.
    ///     When we pack files in parallel, it is possible that files will be ready to be included in the resulting
    ///     file out of order. This means that we need to know the order of any given string ahead of time.
    /// </remarks>
    public Dictionary<string, int> FileNameToIndexDictionary = null!;

    /// <summary>
    ///     Current block in ToC builder.
    /// </summary>
    public int CurrentBlock;

    /// <summary>
    ///     Version between 0 and 15.
    ///     Note: In serialized file this is stored in header.
    /// </summary>
    public ArchiveVersion Version = ArchiveVersion.V0;

    /// <summary>
    ///     This table of contents contains blocks which can create chunks.
    /// </summary>
    public bool CanCreateChunks;

    /// <summary>
    ///     Compressed strings in the StringPool.
    /// </summary>
    private readonly ArrayRentalSlice _poolData;

    /// <summary>
    ///     Currently modified file entry.
    /// </summary>
    private int _currentFile;

    /// <summary>
    ///     Initializes a Table of Contents from given set of items.
    /// </summary>
    /// <param name="blocks">The blocks that will be burned into the ToC.</param>
    /// <param name="files">The files to be packed.</param>
    public TableOfContentsBuilder(List<IBlock<T>> blocks, Span<T> files)
    {
        // Note: Files are sorted in-place during pack.
        // TODO: We can generate the PoolData in parallel; which could save us ~10ms on packing huge 1500+ file archives.
        _poolData = StringPool.Pack(files);
        Init(blocks, files, _poolData.Length);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _poolData.Dispose();
        GC.SuppressFinalize(this);
    }

    ~TableOfContentsBuilder() => Dispose();

    private void Init(List<IBlock<T>> blocks, Span<T> relativeFilePaths, int poolSize)
    {
        // Set ToC version based on biggest decompressed file size.
        ulong largestFileSize = 0;
        foreach (var block in blocks)
        {
            var largest = block.LargestItemSize();
            if (largest > largestFileSize)
                largestFileSize = largest;

            if (block.CanCreateChunks())
                CanCreateChunks = true;
        }

        // Note: We could fast-exit here but it is assumed 4GB+ files are exception, not the norm
        // thus it's faster to not check again after setting largestFileSize
        // File above 4GB, use Version 1 archive.
        if (largestFileSize > uint.MaxValue)
            Version = ArchiveVersion.V1;

        // Populate file name dictionary and names.
        var poolPaths = Polyfills.AllocateUninitializedArray<string>(relativeFilePaths.Length);
        FileNameToIndexDictionary = new Dictionary<string, int>(relativeFilePaths.Length);
        for (var x = 0; x < relativeFilePaths.Length; x++)
        {
            var name = relativeFilePaths[x].RelativePath;
            poolPaths.DangerousGetReferenceAt(x) = name;
            FileNameToIndexDictionary[name] = x;
        }

        // TODO: Pooling these arrays might improve performance.
        // Populate ToC.
        Toc = new TableOfContents
        {
            PoolSize = poolSize,
            BlockCompressions = Polyfills.AllocateUninitializedArray<CompressionPreference>(blocks.Count),
            Blocks = Polyfills.AllocateUninitializedArray<BlockSize>(blocks.Count),
            Entries = Polyfills.AllocateUninitializedArray<FileEntry>(relativeFilePaths.Length),
            Pool = poolPaths
        };
    }

    /// <summary>
    ///     Calculates the size of the table, including String Pool.
    /// </summary>
    public int CalculateTableSize() => Toc.CalculateTableSize(Version);

    #region For internal use

    /// <summary>
    ///     Retrieves a file entry, while incrementing its index atomically.
    /// </summary>
    public ref FileEntry GetAndIncrementFileAtomic()
    {
        var index = Interlocked.Increment(ref _currentFile) - 1;
        return ref Toc.Entries.DangerousGetReferenceAt(index);
    }

    /// <summary>
    ///     Increments the current block index, while returning the previous one atomically.
    /// </summary>
    public int GetAndIncrementBlockIndexAtomic() => Interlocked.Increment(ref CurrentBlock) - 1;

    #endregion For internal use

    /// <summary>
    ///     Serializes the ToC to allow reading from binary.
    /// </summary>
    /// <param name="dataPtr">Address of the table of contents.</param>
    /// <param name="offsetInFile">Offset of the table of contents in the .nx file.</param>
    /// <param name="tocSize">Size of table of contents..</param>
    /// <returns>Number of bytes written.</returns>
    /// <remarks>
    ///     To determine needed size of <paramref name="dataPtr" /> and <paramref name="tocSize" />, call
    ///     <see cref="CalculateTableSize" />.
    /// </remarks>
    public unsafe int Build(byte* dataPtr, int offsetInFile, int tocSize) => Toc.Serialize(dataPtr, offsetInFile, tocSize, Version, _poolData.Span);
}
