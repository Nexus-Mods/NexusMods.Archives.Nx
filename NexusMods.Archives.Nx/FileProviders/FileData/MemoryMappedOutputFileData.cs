using System.IO.MemoryMappedFiles;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Utilities;

// ReSharper disable IntroduceOptionalParameters.Global

namespace NexusMods.Archives.Nx.FileProviders.FileData;

/// <summary>
///     Implementation of <see cref="IFileData" /> backed up by memory mapped files.
/// </summary>
[PublicAPI]
public sealed class MemoryMappedOutputFileData : IFileData
{
    /// <inheritdoc />
    public unsafe byte* Data { get; private set; }

    /// <inheritdoc />
    public ulong DataLength { get; private set; }

    private MemoryMappedViewAccessor? _mappedFileView;
    private bool _disposed;

    /// <summary>
    ///     Creates file data backed by a memory mapped file.
    /// </summary>
    /// <param name="file">The memory mapped file.</param>
    /// <param name="start">Offset to start of the file.</param>
    /// <param name="length">Length of the data to map.</param>
    public unsafe MemoryMappedOutputFileData(MemoryMappedFile file, ulong start, ulong length)
    {
        // Create a memory-mapped file
        if (length != 0)
        {
            _mappedFileView = file.CreateViewAccessor((long)start, (long)length, MemoryMappedFileAccess.ReadWrite);
            Data = (byte*)_mappedFileView.SafeMemoryMappedViewHandle.DangerousGetHandle();
            DataLength = length;
            return;
        }

        InitEmpty();
    }

    private unsafe void InitEmpty()
    {
        Data = (byte*)0x0;
        DataLength = 0;
        _mappedFileView = null;
    }

    /// <inheritdoc />
    ~MemoryMappedOutputFileData() => Dispose();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Notes for non-Windows.
        // Don't dispose the view, but dispose the underlying handle.
        // The View is hardcoded to force a synchronous file flush on dispose.
        // We don't want this behaviour in the case of having a lot of small files.

        // We want the Linux VM or Windows Memory Manager to handle the flushing
        // of the data to disk instead asynchronously.

        // For Linux, this would usually occur after ~30 (+ 0-5) seconds
        // on default kernel settings.

        // The VM will safely commit the flushing even if the process dies or
        // segfaults. The only possible risk of data loss is if the whole system
        // loses power, or the kernel crashes; but in most workloads, inclusive
        // of the Nexus App, this should be handled gracefully.

        // Note:
        // The view itself checks the underlying handle during dispose.
        // There's no harm to doing this, even in old runtimes.

        // Note Note:
        // On Windows, flushing the view leads to somewhat asynchronous write in any case.
        // But .NET Runtime does it synchronously on Linux.
        // This actually brings our platforms closer to parity.
        if (Polyfills.IsWindows())
        {
            // On Windows flushing acts as a hint of 'start writing asynchronously now'.
            // so it's desirable to keep the full flush.
            // https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-flushviewoffile#remarks
            _mappedFileView?.Dispose();
        }
        else
        {
            _mappedFileView?.SafeMemoryMappedViewHandle.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
