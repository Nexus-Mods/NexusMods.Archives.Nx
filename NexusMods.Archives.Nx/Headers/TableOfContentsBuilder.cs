using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers.Enums;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Headers.Structs;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Headers;

/// <summary>
///     Class with all of the logic responsible for building the table of contents during a packing operations.
/// </summary>
internal class TableOfContentsBuilder
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
    ///     Currently modified file entry.
    /// </summary>
    public int CurrentFile;

    /// <summary>
    ///     Version between 0 and 15.
    ///     Note: In serialized file this is stored in header.
    /// </summary>
    public ArchiveVersion Version = ArchiveVersion.V0;

    /// <summary>
    ///     Initializes a Table of Contents from given set of items.
    /// </summary>
    /// <param name="blocks">The blocks that will be burned into the ToC.</param>
    /// <param name="files">The files to be packed.</param>
    public TableOfContentsBuilder(List<IBlock<PackerFile>> blocks, Span<PackerFile> files)
    {
        // Note: Files are sorted in-place during pack.
        var pool = StringPool.Pack(files);
        Init(blocks, pool, files);
    }

    private void Init<T>(List<IBlock<PackerFile>> blocks, ArrayRentalSlice pool, Span<T> relativeFilePaths)
        where T : IHasRelativePath
    {
        // TODO: Validate here and set correct ToC version.

        // Populate file name dictionary.
        FileNameToIndexDictionary = new Dictionary<string, int>(relativeFilePaths.Length);
        for (var x = 0; x < relativeFilePaths.Length; x++)
            FileNameToIndexDictionary[relativeFilePaths[x].RelativePath] = x;

        // Populate ToC.
        Toc = new TableOfContents
        {
            BlockCompressions = Polyfills.AllocateUninitializedArray<CompressionPreference>(blocks.Count),
            Blocks = Polyfills.AllocateUninitializedArray<BlockSize>(blocks.Count),
            Entries = Polyfills.AllocateUninitializedArray<FileEntry>(relativeFilePaths.Length),
            PoolData = pool
        };
    }

    /// <summary>
    ///     Calculates the size of the table, minus stringpool.
    /// </summary>
    public int CalculateTableSize() => throw new NotImplementedException();

    /// <summary>
    ///     Call this function from the packer logic once a block has been successfully processed.
    /// </summary>
    public void OnBlockProcessed(IBlock<PackerFile> block, ArrayRentalSlice blockData)
    {
        lock (Toc)
        {
            // Get all items in this block.
            var toc = Toc;

            // Write Block Details
            toc.Blocks[CurrentBlock] = new BlockSize(blockData.Length);
            toc.BlockCompressions[CurrentBlock] = block.Compression;
            CurrentBlock++;

            // Write File Details
            toc.Entries[CurrentFile++] = new FileEntry();

            throw new NotImplementedException();
        }
    }

    /// <summary>
    ///     Serializes the ToC to allow reading from binary.
    /// </summary>
    public ArrayRentalSlice Build() => throw new NotImplementedException();
}
