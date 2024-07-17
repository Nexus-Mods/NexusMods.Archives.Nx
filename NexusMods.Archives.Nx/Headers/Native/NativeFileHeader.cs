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

    /// <summary>
    ///     Minimum size of chunk blocks in the Nx archive.
    /// </summary>
    internal const int BaseChunkSize = 32768;

    private const uint ExpectedMagic = 0x5355584E; // little endian 'SUXN'

    // Data layout.

    /// <summary>
    ///     [u32] 'Magic' header to identify the file.
    /// </summary>
    public uint Magic;

    // Packed Values
    internal uint _headerData;

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
    ///     [u3] Gets or sets the archive version.
    /// </summary>
    public byte Version
    {
        get => (byte)((_headerData >> 29) & 0b111);
        set => _headerData = (_headerData & 0b00011111_11111111_11111111_11111111) | ((uint)value << 29);
    }

    /// <summary>
    ///     [u4] Gets or sets the block size in its encoded raw value.<br/>
    ///     (Blocks are encoded as (<see cref="BaseChunkSize"/> &lt;&lt; blockSize) - 1)
    /// </summary>
    public byte BlockSize
    {
        get => (byte)((_headerData >> 25) & 0b1111);
        set => _headerData = (_headerData & 0b11100001_11111111_11111111_11111111) | ((uint)value << 25);
    }

    /// <summary>
    ///     [u4] Gets or sets the large chunk size in its encoded raw value (0-15).<br/>
    ///     (Chunks are encoded as (16384 &lt;&lt; chunkSize))
    /// </summary>
    public byte ChunkSize
    {
        get => (byte)((_headerData >> 21) & 0b1111);
        set => _headerData = (_headerData & 0b11111110_00011111_11111111_11111111) | ((uint)value << 21);
    }

    /// <summary>
    ///     [u13] Gets or sets the number of compressed pages used to store the entire ToC (incl. compressed stringpool).
    /// </summary>
    public ushort HeaderPageCount
    {
        get => (ushort)((_headerData >> 8) & 0b11111_11111111);
        set => _headerData = (_headerData & 0b11111110_11100000_00000000_11111111) | ((uint)value << 8);
    }

    /// <summary>
    ///     [u8] Gets/sets the 'feature flags' for this structure.
    ///     A feature flag represents an extension to the format, such as storing time/date.<br/>
    /// </summary>
    /// <remarks>
    ///     This is internal until any feature flags are actually implemented.
    /// </remarks>
    internal ushort FeatureFlags
    {
        get => (ushort)(_headerData & 0b11111111);
        set => _headerData = (_headerData & 0b11111110_11111111_11111111_00000000) | value;
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
        get => (4096 << BlockSize) - 1;
        set => BlockSize = (byte)Math.Log((value + 1) >> 12, 2);
    }

    /// <summary>
    ///     Gets or sets the chunk size used to split large files by.
    /// </summary>
    public int ChunkSizeBytes
    {
        get => BaseChunkSize << ChunkSize;
        set => ChunkSize = (byte)Math.Log(value >> 15, 2);
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
        header->_headerData = 0; // Zero out before assigning bits via packing.

        header->Version = (byte)version;
        header->BlockSizeBytes = blockSizeBytes;
        header->ChunkSizeBytes = chunkSizeBytes;
        header->HeaderPageBytes = headerPageCountBytes.RoundUp4096();
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
        _headerData = BinaryPrimitives.ReverseEndianness(_headerData);
    }
}
