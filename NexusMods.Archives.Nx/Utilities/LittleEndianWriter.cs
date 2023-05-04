using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable CS1591

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Utility for writing to a pointer in Little Endian.
/// </summary>
[ExcludeFromCodeCoverage] // Trivial
public unsafe struct LittleEndianWriter
{
    /// <summary>
    ///     Current pointer being written to.
    /// </summary>
    public byte* Ptr;

    public LittleEndianWriter(byte* ptr) => Ptr = ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(short value)
    {
        LittleEndianHelper.Write((short*)Ptr, value);
        Ptr += sizeof(short);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ushort value)
    {
        LittleEndianHelper.Write((ushort*)Ptr, value);
        Ptr += sizeof(ushort);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint value)
    {
        LittleEndianHelper.Write((uint*)Ptr, value);
        Ptr += sizeof(uint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(int value)
    {
        LittleEndianHelper.Write((int*)Ptr, value);
        Ptr += sizeof(int);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(long value)
    {
        LittleEndianHelper.Write((long*)Ptr, value);
        Ptr += sizeof(long);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ulong value)
    {
        LittleEndianHelper.Write((ulong*)Ptr, value);
        Ptr += sizeof(ulong);
    }

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
    ///     Writes value at offset without advancing the stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAtOffset(short value, int offset) => LittleEndianHelper.Write((short*)(Ptr + offset), value);

    /// <summary>
    ///     Writes value at offset without advancing the stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAtOffset(int value, int offset) => LittleEndianHelper.Write((int*)(Ptr + offset), value);

    /// <summary>
    ///     Writes value at offset without advancing the stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAtOffset(long value, int offset) => LittleEndianHelper.Write((long*)(Ptr + offset), value);

    /// <summary>
    ///     Writes value at offset without advancing the stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAtOffset(ulong value, int offset) => LittleEndianHelper.Write((ulong*)(Ptr + offset), value);

    /// <summary>
    ///     Advances the stream by a specified number of bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Seek(int offset) => Ptr += offset;
}
