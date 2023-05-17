using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Headers.Native.Structs;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Headers.Managed;

/// <summary>
///     Entry for the individual file.
/// </summary>
public struct FileEntry // <= Do not change to class. given the way we use this, structs are more efficient.
{
    /// <summary>
    ///     [u64] Hash of the file described in this entry.
    /// </summary>
    public ulong Hash;

    /// <summary>
    ///     [u32/u64] Size of the file after decompression.
    /// </summary>
    public ulong DecompressedSize;

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

    /// <summary>
    /// Calculated via <see cref="DecompressedSize"/> divided by Chunk Size.
    /// </summary>
    /// <param name="chunkSizeBytes">Size of single chunk in archive.</param>
    public int GetChunkCount(int chunkSizeBytes)
    {
        var count = DecompressedSize / (ulong)chunkSizeBytes;
        if (DecompressedSize % (ulong)chunkSizeBytes != 0)
            count += 1;

        return (int)count;
    }

    /// <summary>
    ///     Writes this managed file entry in the format of <see cref="NativeFileEntryV0" />.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAsV0(ref LittleEndianWriter writer)
    {
        writer.WriteAtOffset(Hash, 0);
        writer.WriteAtOffset((int)DecompressedSize, 8);
        writer.WriteAtOffset(new OffsetPathIndexTuple(DecompressedBlockOffset, FilePathIndex, FirstBlockIndex).Data, 12);
        writer.Seek(NativeFileEntryV0.SizeBytes);
    }

    /// <summary>
    ///     Writes this managed file entry in the format of <see cref="NativeFileEntryV1" />.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAsV1(ref LittleEndianWriter writer)
    {
        writer.WriteAtOffset(Hash, 0);
        writer.WriteAtOffset(DecompressedSize, 8);
        writer.WriteAtOffset(new OffsetPathIndexTuple(DecompressedBlockOffset, FilePathIndex, FirstBlockIndex).Data, 16);
        writer.Seek(NativeFileEntryV1.SizeBytes);
    }

    /// <summary>
    ///     Reads this managed file entry from data serialized as <see cref="NativeFileEntryV0" />.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FromReaderV0(ref LittleEndianReader reader)
    {
        Hash = reader.ReadUlongAtOffset(0);
        DecompressedSize = (ulong)reader.ReadIntAtOffset(8);
        var packed = new OffsetPathIndexTuple(reader.ReadLongAtOffset(12));
        packed.CopyTo(ref this);
        reader.Seek(NativeFileEntryV0.SizeBytes);
    }

    /// <summary>
    ///     Reads this managed file entry from data serialized as <see cref="NativeFileEntryV0" />.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FromReaderV1(ref LittleEndianReader reader)
    {
        Hash = reader.ReadUlongAtOffset(0);
        DecompressedSize = reader.ReadUlongAtOffset(8);
        var packed = new OffsetPathIndexTuple(reader.ReadLongAtOffset(16));
        packed.CopyTo(ref this);
        reader.Seek(NativeFileEntryV1.SizeBytes);
    }
}
