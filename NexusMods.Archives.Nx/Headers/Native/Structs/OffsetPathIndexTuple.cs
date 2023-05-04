using NexusMods.Archives.Nx.Headers.Managed;

namespace NexusMods.Archives.Nx.Headers.Native.Structs;

/// <summary>
///     A tuple consisting of: <br />
///     - [u26] DecompressedBlockOffset <br />
///     - [u20] FilePathIndex <br />
///     - [u18] FirstBlockIndex <br />
///     Used in <see cref="INativeFileEntry" /> and friends.
/// </summary>
public struct OffsetPathIndexTuple : IEquatable<OffsetPathIndexTuple>
{
    internal const int SizeBytes = 8;

    internal long _data;

    /// <summary>
    ///     [u26] Offset of the file inside the decompressed block.
    /// </summary>
    public int DecompressedBlockOffset
    {
        get => (int)((_data >> 38) & 0x3FFFFFF); // Extract the first 26 bits (upper bits)
        set => _data = (_data & ~(0x3FFFFFFL << 38)) | ((long)value << 38);
    }

    /// <summary>
    ///     [u20] Index of the file path associated with this file in the StringPool.
    /// </summary>
    public int FilePathIndex
    {
        get => (int)((_data >> 18) & 0xFFFFF); // Extract the next 20 bits
        set => _data = (_data & ~(0xFFFFFL << 18)) | ((long)value << 18);
    }

    /// <summary>
    ///     [u18] Index of the first block associated with this file.
    /// </summary>
    public int FirstBlockIndex
    {
        get => (int)(_data & 0x3FFFF); // Extract the next 18 bits (lower bits)
        // ReSharper disable once RedundantCast
        set => _data = (_data & ~0x3FFFFL) | (long)value;
    }

    /// <summary>
    ///     Method for fast initialization of the tuple.
    /// </summary>
    /// <param name="decompressedBlockOffset">[u26] Offset of decompressed block.</param>
    /// <param name="filePathIndex">[u20] Index of file path in string pool.</param>
    /// <param name="firstBlockIndex">[u18] Index of first block associated with this file.</param>
    public OffsetPathIndexTuple(int decompressedBlockOffset, int filePathIndex, int firstBlockIndex) =>
        // ReSharper disable once RedundantCast
        _data = ((long)decompressedBlockOffset << 38) | ((long)filePathIndex << 18) | (long)firstBlockIndex;

    /// <summary>
    ///     Method for fast initialization of the tuple from raw data.
    /// </summary>
    /// <param name="data">Raw packed data.</param>
    public OffsetPathIndexTuple(long data) => _data = data;

    /// <summary>
    ///     Copy the values of this tuple to a managed <see cref="FileEntry" />.
    /// </summary>
    /// <remarks>
    ///     This was written to avoid a stack spill.
    /// </remarks>
    public void CopyTo(ref FileEntry entry)
    {
        entry.DecompressedBlockOffset = (int)((_data >> 38) & 0x3FFFFFF); // Extract the first 26 bits (upper bits)
        entry.FilePathIndex = (int)((_data >> 18) & 0xFFFFF); // Extract the next 20 bits
        entry.FirstBlockIndex = (int)(_data & 0x3FFFF); // Extract the next 18 bits (lower bits)
    }

    /// <inheritdoc />
    public bool Equals(OffsetPathIndexTuple other) => _data == other._data;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is OffsetPathIndexTuple other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _data.GetHashCode();
}
