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
#if !NET7_0_OR_GREATER
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowEndOfFileException() => throw new EndOfStreamException();
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowTocVersionNotSupported(ArchiveVersion version) => throw new TocVersionNotSupportedException(version);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowNotANexusArchive() => throw new NotANexusArchiveException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowUnsupportedCompressionMethod(CompressionPreference method) => throw new UnsupportedCompressionMethodException(method);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInsufficientStringPoolSizeException(nint poolSize) => throw new InsufficientStringPoolSizeException(
        $"Size of compressed string pool: {poolSize}, exceeds maximum allowable ({StringPool.MaxCompressedSize}).");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowPackerPoolOutOfItems() => throw new OutOfPackerPoolArraysException(
        $"Ran out of PackerPool items. Pool should only allocate as many arrays as there are worker threads. " +
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
///     A <see cref="PackerArrayPool" /> related exception for when we run out of rented arrays.
/// </summary>
public class OutOfPackerPoolArraysException : Exception
{
    /// <inheritdoc />
    public OutOfPackerPoolArraysException(string? message) : base(message) { }
}

/// <summary>
///     Represents an error that occurs when a Table of Contents for an unsupported version is encountered.
/// </summary>
public class TocVersionNotSupportedException : NotSupportedException
{
    /// <summary>
    /// Version of the archive that is not supported.
    /// </summary>
    public ArchiveVersion Version { get; }

    /// <inheritdoc />
    public TocVersionNotSupportedException(ArchiveVersion version)
        : base($"Table of Contents for Version {version} is not supported.") => Version = version;
}

/// <summary>
///     Represents an error that occurs when the file is not a Nexus archive.
/// </summary>
public class NotANexusArchiveException : NotSupportedException
{
    /// <inheritdoc />
    public NotANexusArchiveException()
        : base("This is not a .nx (Nexus) archive.") { }
}

/// <summary>
///     Represents an error that occurs when an unsupported compression method is encountered.
/// </summary>
public class UnsupportedCompressionMethodException : NotSupportedException
{
    /// <summary>
    ///     The unsupported compression method.
    /// </summary>
    public CompressionPreference Method { get; }

    /// <inheritdoc />
    public UnsupportedCompressionMethodException(CompressionPreference method)
        : base($"Unsupported compression method {method}.") => Method = method;
}
