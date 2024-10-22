using System.Buffers.Binary;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Traits;
using NexusMods.Archives.Nx.Utilities;
using static NexusMods.Archives.Nx.Headers.Native.NativeConstants;

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
    ///     The current version of the Nexus Archive Format.
    /// </summary>
    internal const int CurrentArchiveVersion = 1;

    /// <summary>
    ///     Minimum size of chunk blocks in the Nx archive.
    /// </summary>
    internal const int BaseChunkSize = 512;

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
    ///     [u7] Gets or sets the archive version.
    /// </summary>
    public byte Version
    {
        get => (byte)((_headerData >> 25) & 0b1111111);
        set => _headerData = (_headerData & 0b00000001_11111111_11111111_11111111) | ((uint)value << 25);
    }

    /// <summary>
    ///     [u5] Gets or sets the chunk size in its encoded raw value.<br/>
    ///     (Chunks are encoded as (512 &lt;&lt; chunkSize))
    /// </summary>
    public byte ChunkSize
    {
        get => (byte)((_headerData >> 20) & 0b11111);
        set => _headerData = (_headerData & 0b11111110_00001111_11111111_11111111) | ((uint)value << 20);
    }

    /// <summary>
    ///     [u16] Gets or sets the number of 4K pages used to store the entire header (incl. compressed TOC and stringpool).
    /// </summary>
    public ushort HeaderPageCount
    {
        get => (ushort)((_headerData >> 4) & 0xFFFF);
        set => _headerData = (_headerData & 0b11111111_11111111_00000000_00001111) | ((uint)value << 4);
    }

    /// <summary>
    ///     [u4] Gets/sets the 'feature flags' for this structure.
    ///     A feature flag represents an extension to the format, such as storing time/date.<br/>
    /// </summary>
    public byte FeatureFlags
    {
        get => (byte)(_headerData & 0b1111);
        set => _headerData = (_headerData & 0b11111111_11111111_11111111_11110000) | (uint)(value & 0b1111);
    }

    // Note: Not adding a constructor since it could technically be skipped, if not explicitly init'ed by `new`.

    /// <summary>
    ///     Gets or sets the total amount of bytes required to fetch this header and the table of contents.
    /// </summary>
    public int HeaderPageBytes
    {
        get => HeaderPageCount * HeaderPageSize;
        set => HeaderPageCount = (ushort)(value / HeaderPageSize);
    }

    /// <summary>
    ///     Gets or sets the chunk size used to split large files by.
    /// </summary>
    public int ChunkSizeBytes
    {
        get => BaseChunkSize << ChunkSize;
        // ReSharper disable once PossibleLossOfFraction
        set => ChunkSize = (byte)Math.Log(value / BaseChunkSize, 2);
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
    public static unsafe void Init(NativeFileHeader* header, int chunkSizeBytes, int headerPageCountBytes)
    {
        header->Magic = ExpectedMagic;
        header->_headerData = 0; // Zero out before assigning bits via packing.

        header->Version = CurrentArchiveVersion;
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
