using System.Collections.Concurrent;

namespace NexusMods.Archives.Nx.Structs.Blocks;

/// <summary>
///     This represents the shared state used during deduplication.
/// </summary>
public class ChunkedDeduplicationState
{
    /// <summary>
    ///     Contains a mapping of file hashes to pre-assigned block indexes.
    /// </summary>
    private ConcurrentDictionary<ulong, DeduplicatedChunkedFile> _hashToChunkedFileDetails = new();

    /// <summary>
    ///     Contains a set of short hashes (typically of 4096 bytes) that have been seen.
    /// </summary>
    private ConcurrentDictionary<ulong, bool> _shortHashSet = new();

    /// <summary>
    ///     Resets the state of this deduplication state.
    /// </summary>
    internal void Reset()
    {
        _shortHashSet.Clear();
        _hashToChunkedFileDetails.Clear();
    }

    /// <summary>
    ///     Ensures the internal dictionary has a specific capacity.
    /// </summary>
    internal void EnsureCapacity(int numItems)
    {
        _shortHashSet = new ConcurrentDictionary<ulong, bool>(2, numItems);
        _hashToChunkedFileDetails = new ConcurrentDictionary<ulong, DeduplicatedChunkedFile>(2, numItems);
    }

    /// <summary>
    ///     Checks if the 4096-byte hash has been seen before.
    /// </summary>
    /// <param name="hash4096">The hash of the first 4096 bytes of the file.</param>
    /// <returns>True if the hash has been seen before, false otherwise.</returns>
    internal bool HasPotentialDuplicate(ulong hash4096) => _shortHashSet.ContainsKey(hash4096);

    /// <summary>
    ///     Attempts to find a duplicate file based on its full hash.
    /// </summary>
    /// <param name="fullHash">The full hash of the file.</param>
    /// <param name="existingChunkedFile">The existing file details if a duplicate is found.</param>
    /// <returns>True if a duplicate is found, false otherwise.</returns>
    internal bool TryFindDuplicateByFullHash(ulong fullHash, out DeduplicatedChunkedFile existingChunkedFile) =>
        _hashToChunkedFileDetails.TryGetValue(fullHash, out existingChunkedFile);

    /// <summary>
    ///     Adds a new file hash to the deduplication state.
    /// </summary>
    /// <param name="shortHash">The hash of the first 'X' bytes of the file.</param>
    /// <param name="fullHash">The full hash of the file.</param>
    /// <param name="blockIndex">The index of the block containing this file.</param>
    internal void AddFileHash(ulong shortHash, ulong fullHash, int blockIndex)
    {
        _shortHashSet[shortHash] = true;
        _hashToChunkedFileDetails[fullHash] = new DeduplicatedChunkedFile { BlockIndex = blockIndex };
    }
}

/// <summary>
///     Represents a file index to use for deduplication.
/// </summary>
internal struct DeduplicatedChunkedFile
{
    /// <summary>
    ///     The block index of the file.
    /// </summary>
    public int BlockIndex;
}
