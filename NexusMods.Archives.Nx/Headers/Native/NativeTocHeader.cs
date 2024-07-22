using System.Runtime.InteropServices;
using NexusMods.Archives.Nx.Headers.Enums;

namespace NexusMods.Archives.Nx.Headers.Native;

/// <summary>
///     Represents the native structure of the Table of Contents header in little-endian order.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NativeTocHeader
{
    /// <summary>
    ///     Maximum possible size of the string pool.
    /// </summary>
    internal const int MaxStringPoolSize = 16777215;

    /// <summary>
    ///     Raw Packed field containing:
    ///     - Version (2 bits)
    ///     - StringPoolSize (24 bits)
    ///     - BlockCount (18 bits)
    ///     - FileCount (20 bits)
    /// </summary>
    public ulong RawValue;

    /// <summary>
    ///     Creates a native ToC header from the raw packed fields.
    /// </summary>
    public static NativeTocHeader FromRaw(ulong packedFields) =>
        new()
        {
            RawValue = packedFields
        };

    /// <summary>
    ///     [u20] Gets or sets the FileCount (20 bits).
    /// </summary>
    public int FileCount
    {
        get => (int)(RawValue & 0xFFFFF);
        set => RawValue = (RawValue & ~(ulong)0xFFFFF) | ((ulong)value & 0xFFFFF);
    }

    /// <summary>
    ///     [u18] Gets or sets the BlockCount (18 bits).
    /// </summary>
    public int BlockCount
    {
        get => (int)((RawValue >> 20) & 0x3FFFF);
        set => RawValue = (RawValue & ~((ulong)0x3FFFF << 20)) | ((ulong)(value & 0x3FFFF) << 20);
    }

    /// <summary>
    ///     [u24] Gets or sets the StringPoolSize (24 bits).
    /// </summary>
    public int StringPoolSize
    {
        get => (int)((RawValue >> 38) & 0xFFFFFF);
        set => RawValue = (RawValue & ~((ulong)0xFFFFFF << 38)) | ((ulong)(value & 0xFFFFFF) << 38);
    }

    /// <summary>
    ///     [u2] Gets or sets the Version (2 bits).
    /// </summary>
    public TableOfContentsVersion Version
    {
        get => (TableOfContentsVersion)((RawValue >> 62) & 0x3);
        set => RawValue = (RawValue & ~((ulong)0x3 << 62)) | ((ulong)((uint)value & 0x3) << 62);
    }
}
