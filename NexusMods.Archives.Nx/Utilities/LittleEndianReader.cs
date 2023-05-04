using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable CS1591

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Utility for writing to a pointer in Little Endian.
/// </summary>
[ExcludeFromCodeCoverage] // Trivial
public unsafe struct LittleEndianReader
{
    /// <summary>
    ///     Current pointer being written to.
    /// </summary>
    public byte* Ptr;

    public LittleEndianReader(byte* ptr) => Ptr = ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort()
    {
        var result = LittleEndianHelper.Read((short*)Ptr);
        Ptr += sizeof(short);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort()
    {
        var result = LittleEndianHelper.Read((ushort*)Ptr);
        Ptr += sizeof(ushort);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt()
    {
        var result = LittleEndianHelper.Read((uint*)Ptr);
        Ptr += sizeof(uint);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt()
    {
        var result = LittleEndianHelper.Read((int*)Ptr);
        Ptr += sizeof(int);
        return result;
    }

    /*
         Note: See equivalent section in LittleEndianWriter.
    */

    /// <summary>
    ///     Reads value from offset without advancing the stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShortAtOffset(int offset) => LittleEndianHelper.Read((short*)(Ptr + offset));

    /// <summary>
    ///     Reads value from offset without advancing the stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadIntAtOffset(int offset) => LittleEndianHelper.Read((int*)(Ptr + offset));

    /// <summary>
    ///     Reads value from offset without advancing the stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLongAtOffset(int offset) => LittleEndianHelper.Read((long*)(Ptr + offset));

    /// <summary>
    ///     Reads value from offset without advancing the stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUlongAtOffset(int offset) => LittleEndianHelper.Read((ulong*)(Ptr + offset));

    /// <summary>
    ///     Advances the stream by a specified number of bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Seek(int offset) => Ptr += offset;
}
