using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Headers.Enums;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Headers.Native.Structs;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.Archives.Nx.Headers.Native;

/// <summary>
///     Structure that represents the native serialized file entry.
/// </summary>
/// <remarks>
///     V0 represents <see cref="ArchiveVersion.V0" />.
/// </remarks>
[PublicAPI]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NativeFileEntryV0 : INativeFileEntry, IEquatable<NativeFileEntryV0>
{
    /// <summary>
    ///     Size of item in bytes.
    /// </summary>
    internal const int SizeBytes = 20;

    // Field layout.

    /// <summary>
    ///     [u64] Hash of the file described in this entry.
    /// </summary>
    public ulong Hash;

    /// <summary>
    ///     [u32] Size of the file after decompression.
    /// </summary>
    public uint DecompressedSize;

    private OffsetPathIndexTuple _offsetPathIndexTuple;

    // Properties

    /// <summary>
    ///     [u64] Returns the current entry hash as a ValueObject.
    /// </summary>
    public Hash AsHash => Hashing.xxHash64.Hash.From(Hash);

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
        DecompressedSize = (uint)entry.DecompressedSize;
        _offsetPathIndexTuple =
            new OffsetPathIndexTuple(entry.DecompressedBlockOffset, entry.FilePathIndex, entry.FirstBlockIndex);
    }

    /// <inheritdoc />
    public void CopyTo(ref FileEntry entry)
    {
        entry.Hash = Hash;
        entry.DecompressedSize = DecompressedSize;
        _offsetPathIndexTuple.CopyTo(ref entry);
    }

    /// <summary>
    ///     Determines if two instances of <see cref="NativeFileEntryV0" /> are equal.
    /// </summary>
    /// <param name="left">The left-hand side of the comparison.</param>
    /// <param name="right">The right-hand side of the comparison.</param>
    /// <returns>True if the instances are equal, otherwise False.</returns>
    public static bool operator ==(NativeFileEntryV0 left, NativeFileEntryV0 right) => left.Equals(right);

    /// <summary>
    ///     Determines if two instances of <see cref="NativeFileEntryV0" /> are not equal.
    /// </summary>
    /// <param name="left">The left-hand side of the comparison.</param>
    /// <param name="right">The right-hand side of the comparison.</param>
    /// <returns>True if the instances are not equal, otherwise False.</returns>
    public static bool operator !=(NativeFileEntryV0 left, NativeFileEntryV0 right) => !left.Equals(right);

    /// <inheritdoc />
    [ExcludeFromCodeCoverage] // Autogenerated
    public bool Equals(NativeFileEntryV0 other) => Hash == other.Hash && DecompressedSize == other.DecompressedSize &&
                                                   _offsetPathIndexTuple.Equals(other._offsetPathIndexTuple);

    /// <inheritdoc />
    [ExcludeFromCodeCoverage] // Autogenerated
    public override bool Equals(object? obj) => obj is NativeFileEntryV0 other && Equals(other);

    /// <inheritdoc />
    [ExcludeFromCodeCoverage] // Autogenerated
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
