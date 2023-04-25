using System.Buffers.Binary;
using System.Runtime.InteropServices;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Headers.Native;

/// <summary>
///     Structure that represents the native serialized file header.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)] // We control alignment :)
public struct NativeFileHeader
{
    /// <summary>
    /// Size of header in bytes.
    /// </summary>
    internal const int SizeBytes = 8;
    
    internal const uint ExpectedMagic = 0x5355584E; // little endian 'SUXN'

    // Data layout.
    
    /// <summary>
    ///     [u32] 'Magic' header to identify the file.
    /// </summary>
    public uint Magic;

    // Packed Values
    private byte _versionAndBlockSize;
    internal ushort _largeChunkSizeAndPageCount;

    /// <summary>
    ///     [u8] Reserved.
    /// </summary>
    public readonly byte FeatureFlags;

    // Get/Setters
    
    /// <summary>
    ///     Returns true if the 'Magic' in the header is valid, else false.
    /// </summary>
    public bool IsValidMagicHeader() => Magic == ExpectedMagic;

    /// <summary>
    ///     Sets the magic header.
    /// </summary>
    public void SetMagic() => Magic = ExpectedMagic.AsLittleEndian();

    /// <summary>
    ///     [u4] Gets or sets the archive version.
    /// </summary>
    public byte Version
    {
        get => (byte)((_versionAndBlockSize >> 4) & 0xF); // Extract the first 4 bits (upper bits)
        set => _versionAndBlockSize = (byte)((_versionAndBlockSize & 0x0F) | (value << 4));
    }

    /// <summary>
    ///     [u4] Gets or sets the block size in its encoded raw value.<br />
    ///     (Blocks are encoded as (32768 &lt;&lt; blockSize) - 1)
    /// </summary>
    public byte BlockSize
    {
        get => (byte)(_versionAndBlockSize & 0xF); // Extract the next 4 bits (lower bits)
        set => _versionAndBlockSize = (byte)((_versionAndBlockSize & 0xF0) | value);
    }

    /// <summary>
    ///     [u3] Gets or sets the large chunk size in its encoded raw value (0-7).<br />
    ///     (Blocks are encoded as (32768 &lt;&lt; blockSize) - 1)
    /// </summary>
    public byte ChunkSize
    {
        get => (byte)((_largeChunkSizeAndPageCount >> 13) & 0b111); // Extract the first 3 bits (upper bits)
        set => _largeChunkSizeAndPageCount = (ushort)((_largeChunkSizeAndPageCount & 0x1FFF) | (value << 13));
    }

    /// <summary>
    ///     [u13] Gets or sets the number of compressed pages to store the entire ToC (incl. compressed stringpool).<br />
    ///     (Blocks are encoded as (32768 &lt;&lt; blockSize) - 1)
    /// </summary>
    public ushort TocPageCount
    {
        get => (ushort)(_largeChunkSizeAndPageCount & 0x1FFF); // Extract the next 13 bits (lower bits)
        set => _largeChunkSizeAndPageCount = (ushort)((_largeChunkSizeAndPageCount & 0xE000) | value);
    }
    
    // Note: Not adding a constructor since it could technically be skipped, if not explicitly init'ed by `new`.

    /// <summary>
    ///     Reverses the endian of the data (on a big endian machine, if required).
    /// </summary>
    /// <remarks>
    ///     Only call this method once, or endian will be reversed again.
    /// </remarks>
    public void ReverseEndianIfNeeded()
    {
        // This branch is compiled out.
        if (BitConverter.IsLittleEndian)
            return;

        ReverseEndian();
    }
    
    internal void ReverseEndian()
    {
        Magic = BinaryPrimitives.ReverseEndianness(Magic);
        _largeChunkSizeAndPageCount = BinaryPrimitives.ReverseEndianness(_largeChunkSizeAndPageCount);
    }
}
