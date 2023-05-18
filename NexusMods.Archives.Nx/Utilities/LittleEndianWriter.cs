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
public unsafe struct LittleEndianWriter
{
    /// <summary>
    ///     Current pointer being written to.
    /// </summary>
    public byte* Ptr;

    /// <summary>
    ///     Creates a simple wrapper around a pointer that writes in Little Endian.
    /// </summary>
    /// <param name="ptr">Pointer to the item behind the writer.</param>
    public LittleEndianWriter(byte* ptr) => Ptr = ptr;

    /// <summary>
    ///     Writes a signed 16-bit integer value to the current pointer and advances the pointer.
    /// </summary>
    /// <param name="value">The signed 16-bit integer value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(short value)
    {
        LittleEndianHelper.Write((short*)Ptr, value);
        Ptr += sizeof(short);
    }

    /// <summary>
    ///     Writes an unsigned 16-bit integer value to the current pointer and advances the pointer.
    /// </summary>
    /// <param name="value">The unsigned 16-bit integer value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ushort value)
    {
        LittleEndianHelper.Write((ushort*)Ptr, value);
        Ptr += sizeof(ushort);
    }

    /// <summary>
    ///     Writes an unsigned 32-bit integer value to the current pointer and advances the pointer.
    /// </summary>
    /// <param name="value">The unsigned 32-bit integer value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint value)
    {
        LittleEndianHelper.Write((uint*)Ptr, value);
        Ptr += sizeof(uint);
    }

    /// <summary>
    ///     Writes a signed 32-bit integer value to the current pointer and advances the pointer.
    /// </summary>
    /// <param name="value">The signed 32-bit integer value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(int value)
    {
        LittleEndianHelper.Write((int*)Ptr, value);
        Ptr += sizeof(int);
    }

    /// <summary>
    ///     Writes a signed 64-bit integer value to the current pointer and advances the pointer.
    /// </summary>
    /// <param name="value">The signed 64-bit integer value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(long value)
    {
        LittleEndianHelper.Write((long*)Ptr, value);
        Ptr += sizeof(long);
    }

    /// <summary>
    ///     Writes an unsigned 64-bit integer value to the current pointer and advances the pointer.
    /// </summary>
    /// <param name="value">The unsigned 64-bit integer value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ulong value)
    {
        LittleEndianHelper.Write((ulong*)Ptr, value);
        Ptr += sizeof(ulong);
    }

    /// <summary>
    ///     Writes a byte array to the current pointer and advances the pointer.
    /// </summary>
    /// <param name="data">The byte array to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Span<byte> data)
    {
        fixed (byte* dataPtr = data)
        {
            Unsafe.CopyBlockUnaligned(Ptr, dataPtr, (uint)data.Length);
            Ptr += data.Length;
        }
    }

    /*
        About the Methods Below:
        
            These are for faster writes of structs. 
            
            While they don't reduce the instruction count by much (at all);
            they reduce the dependence of future instructions on earlier instructions,
            (future write does not need to wait for updated value of `Ptr`).
            
            This allows for better pipelining.
            
            Also the JIT can see `offset` parameters as constant when specified as constants
            and optimise that out accordingly.
    */

    /// <summary>
    ///     Writes a signed 16-bit integer value to the specified offset without advancing the pointer.
    /// </summary>
    /// <param name="value">The signed 16-bit integer value to write.</param>
    /// <param name="offset">The offset at which to write the value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAtOffset(short value, int offset) => LittleEndianHelper.Write((short*)(Ptr + offset), value);

    /// <summary>
    ///     Writes a signed 32-bit integer value to the specified offset without advancing the pointer.
    /// </summary>
    /// <param name="value">The signed 32-bit integer value to write.</param>
    /// <param name="offset">The offset at which to write the value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAtOffset(int value, int offset) => LittleEndianHelper.Write((int*)(Ptr + offset), value);

    /// <summary>
    ///     Writes a signed 64-bit integer value to the specified offset without advancing the pointer.
    /// </summary>
    /// <param name="value">The signed 64-bit integer value to write.</param>
    /// <param name="offset">The offset at which to write the value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAtOffset(long value, int offset) => LittleEndianHelper.Write((long*)(Ptr + offset), value);

    /// <summary>
    ///     Writes an unsigned 64-bit integer value to the specified offset without advancing the pointer.
    /// </summary>
    /// <param name="value">The unsigned 64-bit integer value to write.</param>
    /// <param name="offset">The offset at which to write the value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAtOffset(ulong value, int offset) => LittleEndianHelper.Write((ulong*)(Ptr + offset), value);

    /// <summary>
    ///     Advances the stream by a specified number of bytes.
    /// </summary>
    /// <param name="offset">The number of bytes to advance by.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Seek(int offset) => Ptr += offset;
}
