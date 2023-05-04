using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Enums;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Utilities for high performance exception throwing.
/// </summary>
internal static class ThrowHelpers
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowEndOfFileException() =>
        throw new EndOfStreamException();
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInsufficientStringPoolSizeException(nint poolSize) =>
        throw new InsufficientStringPoolSizeException(
            $"Size of compressed string pool: {poolSize}, exceeds maximum allowable ({StringPool.MaxCompressedSize}).");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowTocVersionNotSupported(ArchiveVersion version) =>
        throw new NotSupportedException($"Table of Contents for Version {version} is not supported.");
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowUnsupportedCompressionMethod(CompressionPreference method) =>
        throw new NotSupportedException($"Unsupported compression method {method}.");
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowPackerPoolOutOfItems() =>
        throw new OutOfPackerPoolArraysException($"Ran out of PackerPool items. Pool should only allocate as many arrays as there are worker threads. " +
                                                 $"This is indicative of a potential bug in the code.");
}

/// <summary>
///     This exception is thrown when the <see cref="StringPool" /> cannot be serialized due to the sum of all the
///     uncompressed relative paths
///     being too great.
/// </summary>
public class InsufficientStringPoolSizeException : Exception
{
    /// <inheritdoc />
    public InsufficientStringPoolSizeException(string? message) : base(message) { }
}

/// <summary>
///     A <see cref="PackerArrayPool"/> related exception for when we run out of rented arrays.
/// </summary>
public class OutOfPackerPoolArraysException : Exception
{
    /// <inheritdoc />
    public OutOfPackerPoolArraysException(string? message) : base(message) { }
}
