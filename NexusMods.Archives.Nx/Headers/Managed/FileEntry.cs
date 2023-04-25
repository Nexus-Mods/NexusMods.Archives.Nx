namespace NexusMods.Archives.Nx.Headers.Managed;

/// <summary>
///     Entry for the individual file.
/// </summary>
public struct FileEntry // <= Do not change to class. given the way we use this, structs are more efficient.
{
    /// <summary>
    ///     [u64] Hash of the file described in this entry.
    /// </summary>
    public long Hash;

    /// <summary>
    ///     [u32/u64] Size of the file after decompression.
    /// </summary>
    public long DecompressedSize;

    /// <summary>
    ///     [u26] Offset of the file inside the decompressed block.
    /// </summary>
    public int DecompressedBlockOffset;

    /// <summary>
    ///     [u20] Index of the file path associated with this file in the StringPool.
    /// </summary>
    public int FilePathIndex;

    /// <summary>
    ///     [u18] Index of the first block associated with this file.
    /// </summary>
    public int FirstBlockIndex;
}
