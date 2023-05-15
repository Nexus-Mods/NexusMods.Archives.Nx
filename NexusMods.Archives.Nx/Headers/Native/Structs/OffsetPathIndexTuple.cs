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
    /// <summary>
    /// Size of this structure, in bytes.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    internal const int SizeBytes = 8;
    
    internal long Data;

    /// <summary>
    ///     [u26] Offset of the file inside the decompressed block.
    /// </summary>
    public int DecompressedBlockOffset
    {
        get => (int)((Data >> 38) & 0x3FFFFFF); // Extract the first 26 bits (upper bits)
        set => Data = (Data & ~(0x3FFFFFFL << 38)) | ((long)value << 38);
    }

    /// <summary>
    ///     [u20] Index of the file path associated with this file in the StringPool.
    /// </summary>
    public int FilePathIndex
    {
        get => (int)((Data >> 18) & 0xFFFFF); // Extract the next 20 bits
        set => Data = (Data & ~(0xFFFFFL << 18)) | ((long)value << 18);
    }

    /// <summary>
    ///     [u18] Index of the first block associated with this file.
    /// </summary>
    public int FirstBlockIndex
    {
        get => (int)(Data & 0x3FFFF); // Extract the next 18 bits (lower bits)
        // ReSharper disable once RedundantCast
        set => Data = (Data & ~0x3FFFFL) | (long)value;
    }

    /// <summary>
    ///     Method for fast initialization of the tuple.
    /// </summary>
    /// <param name="decompressedBlockOffset">[u26] Offset of decompressed block.</param>
    /// <param name="filePathIndex">[u20] Index of file path in string pool.</param>
    /// <param name="firstBlockIndex">[u18] Index of first block associated with this file.</param>
    public OffsetPathIndexTuple(int decompressedBlockOffset, int filePathIndex, int firstBlockIndex) =>
        // ReSharper disable once RedundantCast
        Data = ((long)decompressedBlockOffset << 38) | ((long)filePathIndex << 18) | (long)firstBlockIndex;

    /// <summary>
    ///     Method for fast initialization of the tuple from raw data.
    /// </summary>
    /// <param name="data">Raw packed data.</param>
    public OffsetPathIndexTuple(long data) => Data = data;

    /// <summary>
    ///     Copy the values of this tuple to a managed <see cref="FileEntry" />.
    /// </summary>
    /// <remarks>
    ///     This was written to avoid a stack spill.
    /// </remarks>
    public void CopyTo(ref FileEntry entry)
    {
        entry.DecompressedBlockOffset = (int)((Data >> 38) & 0x3FFFFFF); // Extract the first 26 bits (upper bits)
        entry.FilePathIndex = (int)((Data >> 18) & 0xFFFFF); // Extract the next 20 bits
        entry.FirstBlockIndex = (int)(Data & 0x3FFFF); // Extract the next 18 bits (lower bits)
    }
    
    /// <summary>
    /// Determines if two instances of <see cref="OffsetPathIndexTuple"/> are equal.
    /// </summary>
    /// <param name="left">The left-hand side of the comparison.</param>
    /// <param name="right">The right-hand side of the comparison.</param>
    /// <returns>True if the instances are equal, otherwise False.</returns>
    public static bool operator ==(OffsetPathIndexTuple left, OffsetPathIndexTuple right) => left.Equals(right);
    
    /// <summary>
    /// Determines if two instances of <see cref="OffsetPathIndexTuple"/> are not equal.
    /// </summary>
    /// <param name="left">The left-hand side of the comparison.</param>
    /// <param name="right">The right-hand side of the comparison.</param>
    /// <returns>True if the instances are not equal, otherwise False.</returns>
    public static bool operator !=(OffsetPathIndexTuple left, OffsetPathIndexTuple right) => !left.Equals(right);

    /// <inheritdoc />
    public bool Equals(OffsetPathIndexTuple other) => Data == other.Data;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is OffsetPathIndexTuple other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Data.GetHashCode();
}
