namespace NexusMods.Archives.Nx.Headers.Native.Structs;

/// <summary>
/// A tuple consisting of: <br/>
/// - [u26] DecompressedBlockOffset <br/>
/// - [u20] FilePathIndex <br/>
/// - [u18] FirstBlockIndex <br/>
///
/// Used in <see cref="INativeFileEntry"/> and friends.
/// </summary>
public struct OffsetPathIndexTuple : IEquatable<OffsetPathIndexTuple>
{
    internal const int SizeBytes = 8;

    private long _data;

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
    
    /// <inheritdoc />
    public bool Equals(OffsetPathIndexTuple other) => _data == other._data;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is OffsetPathIndexTuple other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _data.GetHashCode();
}
