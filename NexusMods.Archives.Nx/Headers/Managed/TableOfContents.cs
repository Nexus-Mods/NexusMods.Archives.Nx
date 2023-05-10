using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers.Enums;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Headers.Structs;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Headers.Managed;

/// <summary>
///     Managed representation of the deserialized table of contents.
/// </summary>
public class TableOfContents : IEquatable<TableOfContents>
{
    // pointers & non-primitives

    /// <summary>
    ///     Used formats for compression of each block.
    /// </summary>
    public CompressionPreference[] BlockCompressions { get; init; } = null!; // required

    /// <summary>
    ///     Individual block sizes in this structure.
    /// </summary>
    public BlockSize[] Blocks { get; init; } = null!; // required

    /// <summary>
    ///     Individual file entries.
    /// </summary>
    public FileEntry[] Entries { get; init; } = null!; // required

    /// <summary>
    ///     String pool data.
    /// </summary>
    public string[] Pool { get; init; } = null!; // required

    // primitives

    /// <summary>
    ///     Size of the StringPool used to initialize this ToC.
    /// </summary>
    public int PoolSize { get; init; } // required

    /// <summary>
    ///     Deserializes the table of contents from a given address and version.
    /// </summary>
    /// <param name="dataPtr">Pointer to the data stored in the ToC.</param>
    /// <param name="tocSize">Size of table of contents.</param>
    /// <param name="version">Version of the table of contents.</param>
    /// <returns>Deserialized table of contents.</returns>
    public static unsafe T Deserialize<T>(byte* dataPtr, int tocSize, ArchiveVersion version) where T : TableOfContents, new()
    {
        // Re-add padding if it's missing.
        tocSize = (tocSize + sizeof(NativeFileHeader)).RoundUp4096();
        
        var reader = new LittleEndianReader(dataPtr);

        var fileCount = reader.ReadInt();
        var entries = Polyfills.AllocateUninitializedArray<FileEntry>(fileCount);

        var blockCountAndPoolSize = reader.ReadInt();
        var blockCount = blockCountAndPoolSize >> 14;
        var paddingOffset = (blockCountAndPoolSize >> 2) & 0xFFF;
        var blocks = Polyfills.AllocateUninitializedArray<BlockSize>(blockCount);
        var blockCompressions = Polyfills.AllocateUninitializedArray<CompressionPreference>(blockCount);

        // Read Files
        ref var currentEntry = ref entries.DangerousGetReferenceAt(0);
        ref var lastEntry = ref entries.DangerousGetReferenceAt(entries.Length);
        if (version == ArchiveVersion.V0)
        {
            while (Unsafe.IsAddressLessThan(ref currentEntry, ref lastEntry))
            {
                currentEntry.FromReaderV0(ref reader);
                currentEntry = ref Unsafe.Add(ref currentEntry, 1);
            }
        }
        else if (version == ArchiveVersion.V1)
        {
            while (Unsafe.IsAddressLessThan(ref currentEntry, ref lastEntry))
            {
                currentEntry.FromReaderV1(ref reader);
                currentEntry = ref Unsafe.Add(ref currentEntry, 1);
            }
        }
        else
        {
            ThrowHelpers.ThrowTocVersionNotSupported(version);
        }

        // Read block data
        ref var currentBlock = ref blocks.DangerousGetReferenceAt(0);
        ref var currentCompression = ref blockCompressions.DangerousGetReferenceAt(0);
        ref var lastBlock = ref blocks.DangerousGetReferenceAt(blocks.Length);
        ReadBlocksUnrolled(ref currentBlock, ref lastBlock, ref currentCompression, ref reader);

        // Read the StringPool.
        var bytesRead = reader.Ptr - dataPtr;
        var stringPoolSize = tocSize - paddingOffset - bytesRead;
        var pool = StringPool.Unpack(reader.Ptr, (int)stringPoolSize, fileCount);

        return new T
        {
            PoolSize = (int)stringPoolSize,
            Blocks = blocks,
            Entries = entries,
            Pool = pool,
            BlockCompressions = blockCompressions
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReadBlocksUnrolled(ref BlockSize currentBlock, ref BlockSize lastBlock,
        ref CompressionPreference currentCompression, ref LittleEndianReader reader)
    {
        // Unrolling go brrrrrrr.
        // Unroll for 4 iterations at a time
        while (Unsafe.IsAddressLessThan(ref Unsafe.Add(ref currentBlock, 3), ref lastBlock))
        {
            // First iteration
            var value1 = reader.ReadIntAtOffset(0);
            Unsafe.Add(ref currentBlock, 0).CompressedSize = value1 >> 3;
            Unsafe.Add(ref currentCompression, 0) = (CompressionPreference)(value1 & 0x7);

            // Second iteration
            var value2 = reader.ReadIntAtOffset(4);
            Unsafe.Add(ref currentBlock, 1).CompressedSize = value2 >> 3;
            Unsafe.Add(ref currentCompression, 1) = (CompressionPreference)(value2 & 0x7);

            // Third iteration
            var value3 = reader.ReadIntAtOffset(8);
            Unsafe.Add(ref currentBlock, 2).CompressedSize = value3 >> 3;
            Unsafe.Add(ref currentCompression, 2) = (CompressionPreference)(value3 & 0x7);

            // Fourth iteration
            var value4 = reader.ReadIntAtOffset(12);
            Unsafe.Add(ref currentBlock, 3).CompressedSize = value4 >> 3;
            Unsafe.Add(ref currentCompression, 3) = (CompressionPreference)(value4 & 0x7);

            // Advance the reader by 16 bytes
            reader.Seek(16);

            // Increment currentBlock and currentCompression at the end of the loop
            currentBlock = ref Unsafe.Add(ref currentBlock, 4);
            currentCompression = ref Unsafe.Add(ref currentCompression, 4);
        }

        // Process the remaining iterations individually
        while (Unsafe.IsAddressLessThan(ref currentBlock, ref lastBlock))
        {
            var value = reader.ReadInt();
            currentBlock.CompressedSize = value >> 3;
            currentCompression = (CompressionPreference)(value & 0x7);

            currentBlock = ref Unsafe.Add(ref currentBlock, 1);
            currentCompression = ref Unsafe.Add(ref currentCompression, 1);
        }
    }

    /// <summary>
    ///     Serializes the ToC to allow reading from binary.
    /// </summary>
    /// <param name="dataPtr">Memory where to serialize to.</param>
    /// <param name="tocSize">Size of table of contents.</param>
    /// <param name="version">Version of the archive used.</param>
    /// <param name="stringPoolData">Raw data for the string pool.</param>
    /// <returns>Number of bytes written.</returns>
    /// <remarks>To determine needed size of <paramref name="dataPtr" />, call <see cref="CalculateTableSize" />.</remarks>
    public unsafe int Serialize(byte* dataPtr, int tocSize, ArchiveVersion version, Span<byte> stringPoolData)
    {
        // Note: Avoiding bitstreams entirely; manual packing for max perf.
        var writer = new LittleEndianWriter(dataPtr);
        writer.Write(Entries.Length); // limited to 1 mil so int is ok
        var blockCount = Blocks.Length; // Upper 18 bits
        var paddingOffset = (tocSize + sizeof(NativeFileHeader)).RoundUp4096() - tocSize; // Next 12 bits.
        writer.Write((blockCount << 14) | (paddingOffset << 2)); 

        // Now write out all the files.
        // Now let's write a fast loop like the runtime guys do 💜
        ref var currentEntry = ref Entries.DangerousGetReferenceAt(0);
        ref var lastEntry = ref Entries.DangerousGetReferenceAt(Entries.Length);
        if (version == ArchiveVersion.V0)
        {
            while (Unsafe.IsAddressLessThan(ref currentEntry, ref lastEntry))
            {
                currentEntry.WriteAsV0(ref writer);
                currentEntry = ref Unsafe.Add(ref currentEntry, 1);
            }
        }
        else if (version == ArchiveVersion.V1)
        {
            while (Unsafe.IsAddressLessThan(ref currentEntry, ref lastEntry))
            {
                currentEntry.WriteAsV1(ref writer);
                currentEntry = ref Unsafe.Add(ref currentEntry, 1);
            }
        }
        else
        {
            ThrowHelpers.ThrowTocVersionNotSupported(version);
        }

        // Write out all the block infos.
        ref var currentBlock = ref Blocks.DangerousGetReferenceAt(0);
        ref var currentCompression = ref BlockCompressions.DangerousGetReferenceAt(0);
        ref var lastBlock = ref Blocks.DangerousGetReferenceAt(Blocks.Length);
        WriteBlocksUnrolled(ref currentBlock, ref lastBlock, ref currentCompression, ref writer);

        // Write the pool.
        writer.Write(stringPoolData);
        return (int)(writer.Ptr - dataPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteBlocksUnrolled(ref BlockSize currentBlock, ref BlockSize lastBlock,
        ref CompressionPreference currentCompression, ref LittleEndianWriter writer)
    {
        // Any fans of SIMD-style unrolling?

        // Unroll for 4 iterations at a time
        while (Unsafe.IsAddressLessThan(ref Unsafe.Add(ref currentBlock, 3), ref lastBlock))
        {
            // Unrolled part.
            writer.WriteAtOffset((Unsafe.Add(ref currentBlock, 0).CompressedSize << 3) | (int)Unsafe.Add(ref currentCompression, 0), 0);
            writer.WriteAtOffset((Unsafe.Add(ref currentBlock, 1).CompressedSize << 3) | (int)Unsafe.Add(ref currentCompression, 1), 4);
            writer.WriteAtOffset((Unsafe.Add(ref currentBlock, 2).CompressedSize << 3) | (int)Unsafe.Add(ref currentCompression, 2), 8);
            writer.WriteAtOffset((Unsafe.Add(ref currentBlock, 3).CompressedSize << 3) | (int)Unsafe.Add(ref currentCompression, 3), 12);

            // Advance the writer to next unrolled iteration.
            writer.Seek(16);

            // Increment currentBlock and currentCompression at the end of the loop
            currentBlock = ref Unsafe.Add(ref currentBlock, 4);
            currentCompression = ref Unsafe.Add(ref currentCompression, 4);
        }

        // Process the remaining iterations individually without unroll
        while (Unsafe.IsAddressLessThan(ref currentBlock, ref lastBlock))
        {
            writer.Write((currentBlock.CompressedSize << 3) | (int)currentCompression);
            currentBlock = ref Unsafe.Add(ref currentBlock, 1);
            currentCompression = ref Unsafe.Add(ref currentCompression, 1);
        }

        while (Unsafe.IsAddressLessThan(ref currentBlock, ref lastBlock))
        {
            writer.Write((currentBlock.CompressedSize << 3) | (int)currentCompression);
            currentBlock = ref Unsafe.Add(ref currentBlock, 1);
            currentCompression = ref Unsafe.Add(ref currentCompression, 1);
        }
    }

    /// <summary>
    ///     Calculates the size of the table after serialization to binary.
    /// </summary>
    /// <param name="version">Version to serialize into.</param>
    /// <returns>Size of the Table of Contents</returns>
    public int CalculateTableSize(ArchiveVersion version)
    {
        const int headerSize = 8;
        var currentSize = headerSize;
        var entrySize = version switch
        {
            ArchiveVersion.V0 => 20,
            ArchiveVersion.V1 => 24,
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };

        // Add file entries.
        // Add blocks.
        // Add string pool size.
        currentSize += Entries.Length * entrySize;
        currentSize += Blocks.Length * 4;
        currentSize += PoolSize;

        // Round up.
        return currentSize;
    }

    #region Autogenerated Code

    /// <inheritdoc />
    public bool Equals(TableOfContents? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return BlockCompressions.SequenceEqual(other.BlockCompressions) && Blocks.SequenceEqual(other.Blocks) &&
               Entries.SequenceEqual(other.Entries) && Pool.SequenceEqual(other.Pool) && PoolSize == other.PoolSize;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((TableOfContents)obj);
    }

    #endregion

    /// <inheritdoc />
    public override int GetHashCode() => PoolSize;
}
