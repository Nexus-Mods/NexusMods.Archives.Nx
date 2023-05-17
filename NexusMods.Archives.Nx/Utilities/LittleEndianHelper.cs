using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Utility class for reading/writing pointers in Little Endian.
/// </summary>
[ExcludeFromCodeCoverage] // Trivial
internal static unsafe class LittleEndianHelper
{
    // Performance Note: BitConverter.IsLittleEndian is intrinsic (and therefore constant).  
    // The JIT will remove branches out featuring this check entirely, so all the functions here 
    // are no-op for Little-Endian.

    // In addition, BinaryPrimitives are implemented as intrinsics on newer runtimes and will use dedicated
    // CPU instructions if available; making the operation fast.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short Read(short* value)
    {
        if (BitConverter.IsLittleEndian)
            return *value;

        return BinaryPrimitives.ReverseEndianness(*value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort Read(ushort* value)
    {
        if (BitConverter.IsLittleEndian)
            return *value;

        return BinaryPrimitives.ReverseEndianness(*value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Read(uint* value)
    {
        if (BitConverter.IsLittleEndian)
            return *value;

        return BinaryPrimitives.ReverseEndianness(*value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Read(int* value)
    {
        if (BitConverter.IsLittleEndian)
            return *value;

        return BinaryPrimitives.ReverseEndianness(*value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Read(long* value)
    {
        if (BitConverter.IsLittleEndian)
            return *value;

        return BinaryPrimitives.ReverseEndianness(*value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Read(ulong* value)
    {
        if (BitConverter.IsLittleEndian)
            return *value;

        return BinaryPrimitives.ReverseEndianness(*value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(short* address, short value)
    {
        if (BitConverter.IsLittleEndian)
        {
            *address = value;
            return;
        }

        *address = BinaryPrimitives.ReverseEndianness(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(ushort* address, ushort value)
    {
        if (BitConverter.IsLittleEndian)
        {
            *address = value;
            return;
        }

        *address = BinaryPrimitives.ReverseEndianness(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(uint* address, uint value)
    {
        if (BitConverter.IsLittleEndian)
        {
            *address = value;
            return;
        }

        *address = BinaryPrimitives.ReverseEndianness(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(int* address, int value)
    {
        if (BitConverter.IsLittleEndian)
        {
            *address = value;
            return;
        }

        *address = BinaryPrimitives.ReverseEndianness(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(ulong* address, ulong value)
    {
        if (BitConverter.IsLittleEndian)
        {
            *address = value;
            return;
        }

        *address = BinaryPrimitives.ReverseEndianness(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(long* address, long value)
    {
        if (BitConverter.IsLittleEndian)
        {
            *address = value;
            return;
        }

        *address = BinaryPrimitives.ReverseEndianness(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AsLittleEndian(this uint value)
    {
        if (BitConverter.IsLittleEndian)
            return value;

        return BinaryPrimitives.ReverseEndianness(value);
    }

    /*
    // Commented out unused to avoid polluting dead code in DLL (and also not annoy R#/Rider). Uncomment as needed.
     
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short AsLittleEndian(this short value)
    {
        if (BitConverter.IsLittleEndian)
            return value;

        return BinaryPrimitives.ReverseEndianness(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort AsLittleEndian(this ushort value)
    {
        if (BitConverter.IsLittleEndian)
            return value;

        return BinaryPrimitives.ReverseEndianness(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int AsLittleEndian(this int value)
    {
        if (BitConverter.IsLittleEndian)
            return value;

        return BinaryPrimitives.ReverseEndianness(value);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long AsLittleEndian(this long value)
    {
        if (BitConverter.IsLittleEndian)
            return value;

        return BinaryPrimitives.ReverseEndianness(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong AsLittleEndian(this ulong value)
    {
        if (BitConverter.IsLittleEndian)
            return value;

        return BinaryPrimitives.ReverseEndianness(value);
    }
    */
}
