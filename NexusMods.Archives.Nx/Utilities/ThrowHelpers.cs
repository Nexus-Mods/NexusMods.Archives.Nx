using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Enums;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Headers.Native;

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
    public static void ThrowTocVersionNotSupported(TableOfContentsVersion version) => throw new TocVersionNotSupportedException(version);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowNotANexusArchive() => throw new NotANexusArchiveException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void EntryCannotFitInArray(FileEntry entry) => throw new EntryCannotFitInArrayException(entry);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowUnsupportedCompressionMethod(CompressionPreference method) => throw new UnsupportedCompressionMethodException(method);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInsufficientStringPoolSizeException(nint poolSize) => throw new InsufficientStringPoolSizeException(
        $"Size of compressed string pool: {poolSize}, exceeds maximum allowable ({NativeTocHeader.MaxStringPoolSize}).");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowPackerPoolOutOfItems() => throw new OutOfPackerPoolArraysException(
        $"Ran out of PackerPool items. Pool should only allocate as many arrays as there are worker threads. " +
        $"This is indicative of a potential bug in the code.");

    /// <summary>
    /// Throws an exception when there are too many blocks in the archive.
    /// </summary>
    /// <param name="blockCount">The actual number of blocks.</param>

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowTooManyBlocksException(int blockCount) =>
        throw new TooManyBlocksException(blockCount);

    /// <summary>
    /// Throws an exception when there are too many files in the archive.
    /// </summary>
    /// <param name="fileCount">The actual number of files.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowTooManyFilesException(int fileCount) =>
        throw new TooManyFilesException(fileCount);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowUnsupportedArchiveVersion(byte version) => throw new UnsupportedArchiveVersionException(version);
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
[PublicAPI]
public class TocVersionNotSupportedException : NotSupportedException
{
    /// <summary>
    ///     Version of the archive that is not supported.
    /// </summary>
    public TableOfContentsVersion Version { get; }

    /// <inheritdoc />
    public TocVersionNotSupportedException(TableOfContentsVersion version)
        : base($"Table of Contents for Version {version} is not supported.") => Version = version;
}

/// <summary>
///     Represents an error that occurs when the file is not a Nexus archive.
/// </summary>
[PublicAPI]
public class NotANexusArchiveException : NotSupportedException
{
    /// <inheritdoc />
    public NotANexusArchiveException()
        : base("This is not a .nx (Nexus) archive.") { }
}

/// <summary>
///     Represents an error that occurs when an unsupported compression method is encountered.
/// </summary>
[PublicAPI]
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

/// <summary>
///     Represents an error that occurs when a file entry is too large to fit into a .NET array.
/// </summary>
[PublicAPI]
public class EntryCannotFitInArrayException : ArgumentException
{
    /// <summary>
    ///     The file entry that caused the exception.
    /// </summary>
    public FileEntry Entry { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntryCannotFitInArrayException"/> class.
    /// </summary>
    /// <param name="entry">The file entry that is too large.</param>
    public EntryCannotFitInArrayException(FileEntry entry)
        : base($"This file Entry cannot be extracted into an array because it is too large. .NET Arrays are limited to 2GiB. File Size: {entry.DecompressedSize}")
    {
        Entry = entry;
    }
}

/// <summary>
///     Exception thrown when the number of blocks in a Nx archive exceeds the maximum limit
///     supported by the table of contents.
/// </summary>
public class TooManyBlocksException : IOException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TooManyBlocksException"/> class.
    /// </summary>
    /// <param name="blockCount">The actual number of blocks.</param>
    public TooManyBlocksException(int blockCount)
        : base($"Too many blocks: {blockCount}. Maximum allowed is {TableOfContents.MaxBlockCountV0V1}.")
    {
        BlockCount = blockCount;
    }

    /// <summary>
    ///     Gets the actual number of blocks that caused the exception.
    /// </summary>
    public int BlockCount { get; }
}

/// <summary>
///     Thrown when the number of files in a Nx archive exceeds the maximum limit
///     supported by the table of contents.
/// </summary>
public class TooManyFilesException : IOException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TooManyFilesException"/> class.
    /// </summary>
    /// <param name="fileCount">The actual number of files.</param>
    public TooManyFilesException(int fileCount)
        : base($"Too many files: {fileCount}. Maximum allowed is {TableOfContents.MaxFileCountV0V1}.")
    {
        FileCount = fileCount;
    }

    /// <summary>
    ///     Gets the actual number of files that caused the exception.
    /// </summary>
    public int FileCount { get; }
}

/// <summary>
///     Represents an error that occurs when an unsupported archive version is encountered.
/// </summary>
[PublicAPI]
public class UnsupportedArchiveVersionException : NotSupportedException
{
    /// <summary>
    ///     The unsupported archive version.
    /// </summary>
    public byte Version { get; }

    /// <inheritdoc />
    public UnsupportedArchiveVersionException(byte version)
        : base($"Unsupported archive version {version}.\n" +
               $"The most recent supported version is {NativeFileHeader.CurrentArchiveVersion}.\n" +
               $"Please update your library.")
        => Version = version;
}
