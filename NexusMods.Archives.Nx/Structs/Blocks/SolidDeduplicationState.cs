namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     This represents the shared state used during deduplication.
/// </summary>
public class SolidDeduplicationState
{
    /// <summary>
    ///     Contains a mapping of file hashes to pre-assigned block indexes.
    /// </summary>
    private Dictionary<ulong, DeduplicatedSolidFile> _hashToSolidFileDetails = new();

    /// <summary>
    ///     Resets the state of this deduplication state.
    /// </summary>
    internal void Reset()
    {
        _hashToSolidFileDetails.Clear();
    }

    /// <summary>
    ///     Ensures the internal dictionary has a specific capacity.
    /// </summary>
    internal void EnsureCapacity(int numItems) => _hashToSolidFileDetails.EnsureCapacity(numItems);

    /// <summary>
    ///     Attempts to find a duplicate file based on its full hash.
    /// </summary>
    /// <param name="fullHash">The full hash of the file.</param>
    /// <param name="existingSolidFile">The existing file details if a duplicate is found.</param>
    /// <returns>True if a duplicate is found, false otherwise.</returns>
    internal bool TryFindDuplicateByFullHash(ulong fullHash, out DeduplicatedSolidFile existingSolidFile) =>
        _hashToSolidFileDetails.TryGetValue(fullHash, out existingSolidFile);

    /// <summary>
    ///     Adds a new file hash to the deduplication state.
    /// </summary>
    /// <param name="fullHash">The full hash of the file.</param>
    /// <param name="blockIndex">The index of the block containing this file.</param>
    /// <param name="decompressedOffset">Offset of the decompressed file in the block.</param>
    internal void AddFileHash(ulong fullHash, int blockIndex, int decompressedOffset)
    {
        _hashToSolidFileDetails[fullHash] = new DeduplicatedSolidFile
        {
            BlockIndex = blockIndex,
            DecompressedBlockOffset = decompressedOffset
        };
    }
}

/// <summary>
///     Represents a file index to use for deduplication.
/// </summary>
internal struct DeduplicatedSolidFile
{
    /// <summary>
    ///     The block index of the file.
    /// </summary>
    public int BlockIndex;

    /// <summary>
    ///     Offset into the decompressed block.
    /// </summary>
    public int DecompressedBlockOffset;
}
