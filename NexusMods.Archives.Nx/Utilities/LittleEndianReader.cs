using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

#pragma warning disable CS1591

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Utility for writing to a pointer in Little Endian.
/// </summary>
[PublicAPI] // Not really intended to be public, but it's used in APIs that would be useful to public.
[ExcludeFromCodeCoverage] // Trivial
public unsafe struct LittleEndianReader
{
    /// <summary>
    ///     Current pointer being written to.
    /// </summary>
    public byte* Ptr;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LittleEndianReader" /> struct with the given pointer.
    /// </summary>
    /// <param name="ptr">The pointer to read from.</param>
    public LittleEndianReader(byte* ptr) => Ptr = ptr;

    /// <summary>
    ///     Reads a signed 16-bit integer from the current pointer in Little Endian format and advances the pointer.
    /// </summary>
    /// <returns>A signed 16-bit integer value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShort()
    {
        var result = LittleEndianHelper.Read((short*)Ptr);
        Ptr += sizeof(short);
        return result;
    }

    /// <summary>
    ///     Reads an unsigned 16-bit integer from the current pointer in Little Endian format and advances the pointer.
    /// </summary>
    /// <returns>An unsigned 16-bit integer value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUShort()
    {
        var result = LittleEndianHelper.Read((ushort*)Ptr);
        Ptr += sizeof(ushort);
        return result;
    }

    /// <summary>
    ///     Reads an unsigned 32-bit integer from the current pointer in Little Endian format and advances the pointer.
    /// </summary>
    /// <returns>An unsigned 32-bit integer value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt()
    {
        var result = LittleEndianHelper.Read((uint*)Ptr);
        Ptr += sizeof(uint);
        return result;
    }

    /// <summary>
    ///     Reads a signed 32-bit integer from the current pointer in Little Endian format and advances the pointer.
    /// </summary>
    /// <returns>A signed 32-bit integer value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt()
    {
        var result = LittleEndianHelper.Read((int*)Ptr);
        Ptr += sizeof(int);
        return result;
    }

    /// <summary>
    ///     Reads an unsigned 64-bit integer from the current pointer in Little Endian format and advances the pointer.
    /// </summary>
    /// <returns>A unsigned 64-bit integer value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadULong()
    {
        var result = LittleEndianHelper.Read((ulong*)Ptr);
        Ptr += sizeof(ulong);
        return result;
    }

    /*
         Note: See equivalent section in LittleEndianWriter.
    */

    /// <summary>
    ///     Reads a signed 16-bit integer from the specified offset in Little Endian format without advancing the pointer.
    /// </summary>
    /// <param name="offset">The offset in bytes from the current pointer.</param>
    /// <returns>A signed 16-bit integer value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadShortAtOffset(int offset) => LittleEndianHelper.Read((short*)(Ptr + offset));

    /// <summary>
    ///     Reads a signed 32-bit integer from the specified offset in Little Endian format without advancing the pointer.
    /// </summary>
    /// <param name="offset">The offset in bytes from the current pointer.</param>
    /// <returns>A signed 32-bit integer value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadIntAtOffset(int offset) => LittleEndianHelper.Read((int*)(Ptr + offset));

    /// <summary>
    ///     Reads a signed 32-bit integer from the specified offset in Little Endian format without advancing the pointer.
    /// </summary>
    /// <param name="offset">The offset in bytes from the current pointer.</param>
    /// <returns>A signed 32-bit integer value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUIntAtOffset(int offset) => LittleEndianHelper.Read((uint*)(Ptr + offset));

    /// <summary>
    ///     Reads a signed 64-bit integer from the specified offset in Little Endian format without advancing the pointer.
    /// </summary>
    /// <param name="offset">The offset in bytes from the current pointer.</param>
    /// <returns>A signed 64-bit integer value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadLongAtOffset(int offset) => LittleEndianHelper.Read((long*)(Ptr + offset));

    /// <summary>
    ///     Reads an unsigned 64-bit integer from the specified offset in Little Endian format without advancing the pointer.
    /// </summary>
    /// <param name="offset">The offset in bytes from the current pointer.</param>
    /// <returns>An unsigned 64-bit integer value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUlongAtOffset(int offset) => LittleEndianHelper.Read((ulong*)(Ptr + offset));

    /// <summary>
    ///     Advances the pointer by a specified number of bytes.
    /// </summary>
    /// <param name="offset">The number of bytes to advance the pointer by.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Seek(int offset) => Ptr += offset;
}
