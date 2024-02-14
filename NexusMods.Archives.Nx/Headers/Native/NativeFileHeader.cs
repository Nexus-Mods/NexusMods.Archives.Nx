using System.Buffers.Binary;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Headers.Enums;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Headers.Native;

/// <summary>
///     Structure that represents the native serialized file header.
/// </summary>
[PublicAPI]
[StructLayout(LayoutKind.Sequential, Pack = 1)] // We control alignment :)
public struct NativeFileHeader : ICanConvertToLittleEndian
{
    /// <summary>
    ///     Size of header in bytes.
    /// </summary>
    internal const int SizeBytes = 8;

    private const uint ExpectedMagic = 0x5355584E; // little endian 'SUXN'

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
    public byte FeatureFlags;

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
    /// </summary>
    public byte ChunkSize
    {
        get => (byte)((_largeChunkSizeAndPageCount >> 13) & 0b111); // Extract the first 3 bits (upper bits)
        set => _largeChunkSizeAndPageCount = (ushort)((_largeChunkSizeAndPageCount & 0x1FFF) | (value << 13));
    }

    /// <summary>
    ///     [u13] Gets or sets the number of compressed pages to store the entire ToC (incl. compressed stringpool).<br />
    /// </summary>
    public ushort HeaderPageCount
    {
        get => (ushort)(_largeChunkSizeAndPageCount & 0x1FFF); // Extract the next 13 bits (lower bits)
        set => _largeChunkSizeAndPageCount = (ushort)((_largeChunkSizeAndPageCount & 0xE000) | value);
    }

    // Note: Not adding a constructor since it could technically be skipped, if not explicitly init'ed by `new`.

    /// <summary>
    ///     Gets or sets the total amount of bytes required to fetch this header and the table of contents.
    /// </summary>
    public int HeaderPageBytes
    {
        get => HeaderPageCount * 4096;
        set => HeaderPageCount = (ushort)(value >> 12);
    }

    /// <summary>
    ///     Gets or sets the block size of SOLID blocks in this archive.
    /// </summary>
    public int BlockSizeBytes
    {
        get => (32768 << BlockSize) - 1;
        set => BlockSize = (byte)Math.Log((value + 1) >> 15, 2);
    }

    /// <summary>
    ///     Gets or sets the chunk size used to split large files by.
    /// </summary>
    public int ChunkSizeBytes
    {
        get => 1048576 << ChunkSize;
        set => ChunkSize = (byte)Math.Log(value >> 20, 2);
    }

    /// <summary>
    ///     Gets or sets the total amount of bytes taken by table of contents (including padding).
    /// </summary>
    public unsafe int TocSize => HeaderPageBytes - sizeof(NativeFileHeader);

    /// <summary>
    ///     Initializes the header with given data.
    /// </summary>
    /// <remarks>
    ///     For initializing data in native memory. Will reverse endian.
    /// </remarks>
    public static unsafe void Init(NativeFileHeader* header, ArchiveVersion version, int blockSizeBytes, int chunkSizeBytes, int headerPageCountBytes)
    {
        header->Magic = ExpectedMagic;
        header->Version = (byte)version;
        header->BlockSizeBytes = blockSizeBytes;
        header->ChunkSizeBytes = chunkSizeBytes;
        header->HeaderPageBytes = headerPageCountBytes.RoundUp4096();
        header->FeatureFlags = 0;
        header->ReverseEndianIfNeeded();
    }

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
