using NexusMods.Archives.Nx.Headers.Enums;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Headers.Native.Structs;

namespace NexusMods.Archives.Nx.Headers.Native;

/// <summary>
///     Structure that represents the native serialized file entry.
/// </summary>
/// <remarks>
///     V0 represents <see cref="ArchiveVersion.V0" />.
/// </remarks>
public struct NativeFileEntryV0 : INativeFileEntry, IEquatable<NativeFileEntryV0>
{
    // Field layout.

    /// <summary>
    ///     [u64] Hash of the file described in this entry.
    /// </summary>
    public long Hash;

    /// <summary>
    ///     [u32] Size of the file after decompression.
    /// </summary>
    public int DecompressedSize;

    private OffsetPathIndexTuple _offsetPathIndexTuple;

    // Properties

    /// <summary>
    ///     [u26] Offset of the file inside the decompressed block.
    /// </summary>
    public int DecompressedBlockOffset
    {
        get => _offsetPathIndexTuple.DecompressedBlockOffset;
        set => _offsetPathIndexTuple.DecompressedBlockOffset = value;
    }

    /// <summary>
    ///     [u20] Index of the file path associated with this file in the StringPool.
    /// </summary>
    public int FilePathIndex
    {
        get => _offsetPathIndexTuple.FilePathIndex;
        set => _offsetPathIndexTuple.FilePathIndex = value;
    }

    /// <summary>
    ///     [u18] Index of the first block associated with this file.
    /// </summary>
    public int FirstBlockIndex
    {
        get => _offsetPathIndexTuple.FirstBlockIndex;
        set => _offsetPathIndexTuple.FirstBlockIndex = value;
    }

    /// <inheritdoc />
    public void CopyFrom(in FileEntry entry)
    {
        Hash = entry.Hash;
        DecompressedSize = (int)entry.DecompressedSize;
        DecompressedBlockOffset = entry.DecompressedBlockOffset;
        FilePathIndex = entry.FilePathIndex;
        FirstBlockIndex = entry.FirstBlockIndex;
    }

    /// <inheritdoc />
    public void CopyTo(ref FileEntry entry)
    {
        entry.Hash = Hash;
        entry.DecompressedSize = DecompressedSize;
        entry.DecompressedBlockOffset = DecompressedBlockOffset;
        entry.FilePathIndex = FilePathIndex;
        entry.FirstBlockIndex = FirstBlockIndex;
    }
    
    /// <inheritdoc />
    public bool Equals(NativeFileEntryV0 other) => Hash == other.Hash && DecompressedSize == other.DecompressedSize && _offsetPathIndexTuple.Equals(other._offsetPathIndexTuple);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is NativeFileEntryV0 other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Hash.GetHashCode();
            hashCode = (hashCode * 397) ^ DecompressedSize.GetHashCode();
            hashCode = (hashCode * 397) ^ _offsetPathIndexTuple.GetHashCode();
            return hashCode;
        }
    }
}
