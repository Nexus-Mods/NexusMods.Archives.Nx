using System.IO.MemoryMappedFiles;
using JetBrains.Annotations;
using NexusMods.Archives.Nx.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;

// ReSharper disable IntroduceOptionalParameters.Global

namespace NexusMods.Archives.Nx.FileProviders.FileData;

/// <summary>
///     Implementation of <see cref="IFileData" /> backed up by memory mapped files.
/// </summary>
[PublicAPI]
public sealed class MemoryMappedFileData : IFileData
{
    private bool _disposed;

    private MemoryMappedFile? _mappedFile;
    private MemoryMappedViewAccessor? _mappedFileView;

    /// <summary>
    ///     Creates file data backed by a memory mapped file.
    /// </summary>
    /// <param name="filePath">Path of the file to map.</param>
    /// <param name="start">Offset to start of the file.</param>
    /// <param name="length">Length of the data to map.</param>
    public MemoryMappedFileData(string filePath, ulong start, ulong length) : this(filePath, start, length, false) { }

    /// <summary>
    ///     Creates file data backed by a memory mapped file.
    /// </summary>
    /// <param name="filePath">Path of the file to map.</param>
    /// <param name="start">Offset to start of the file.</param>
    /// <param name="length">Length of the data to map.</param>
    /// <param name="readOnly">If true, this is read only.</param>
    public MemoryMappedFileData(string filePath, ulong start, ulong length, bool readOnly)
    {
        // TODO: Investigate if it's worth using OpenExisting in cases of chunked files.
        // Checking if an existing MMF is already there is a perf penalty for opening lots of small files
        // but it would speed up large chunked files. Issue is; we don't know the tradeoff here :p.

        // Create a memory-mapped file
        if (length != 0)
        {
            var fileMode = readOnly ? FileAccess.Read : FileAccess.ReadWrite;
            var mmfAccess = readOnly ? MemoryMappedFileAccess.Read : MemoryMappedFileAccess.ReadWrite;

            var fs = new FileStream(filePath, FileMode.Open, fileMode, FileShare.ReadWrite);
            _mappedFile = MemoryMappedFile.CreateFromFile(fs, null, 0, mmfAccess, HandleInheritability.Inheritable, false);
            InitFromMmf(start, length, readOnly);
            return;
        }

        InitEmpty();
    }

    /// <inheritdoc />
    public unsafe byte* Data { get; private set; }

    /// <inheritdoc />
    public ulong DataLength { get; private set; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _mappedFile?.Dispose();
        _mappedFileView?.Dispose();
        GC.SuppressFinalize(this);
    }

    private unsafe void InitFromMmf(ulong start, ulong length, bool isReadOnly = false)
    {
        var access = isReadOnly ? MemoryMappedFileAccess.Read : MemoryMappedFileAccess.ReadWrite;
        // Note: Truncating cast here, though files of 2^63 bytes are unlikely.
        _mappedFileView = _mappedFile!.CreateViewAccessor((long)start, (long)length, access);
        Data = (byte*)_mappedFileView.SafeMemoryMappedViewHandle.DangerousGetHandle();
        DataLength = length;

        // Provide some OS specific hints
        // POSIX compliant
        if (OperatingSystem.IsLinux())
        {
            // Also tried MADV_SEQUENTIAL, but didn't yield a benefit (on Linux) strangely.
            madvise(Data, (nuint)length, 3); // MADV_WILLNEED
        }
        else if (OperatingSystem.IsAndroid())
        {
            madvise_android(Data, (nuint)length, 3); // MADV_WILLNEED
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsIOS())
        {
            madvise_libSystem(Data, (nuint)length, 3); // MADV_WILLNEED
        }
        else if (OperatingSystem.IsWindows())
        {
            var entries = stackalloc MemoryRangeEntry[1];
            // Note: Ignore the potential overflow on 32-bit OS here, it is not dangerous.
            entries[0] = new MemoryRangeEntry { VirtualAddress = (nint)Data, NumberOfBytes = (nuint)DataLength };
            // ReSharper disable once RedundantCast
            PrefetchVirtualMemory(Process.GetCurrentProcess().Handle, (nuint)1, entries, 0);
        }
    }

    private unsafe void InitEmpty()
    {
        Data = (byte*)0x0;
        DataLength = 0;
        _mappedFile = null;
        _mappedFileView = null;
    }

    /// <inheritdoc />
    ~MemoryMappedFileData() => Dispose();

    #region Memory Access Hints for OSes

    // POSIX Compatible
    [DllImport("libc.so.6", EntryPoint = "madvise")]
    private static extern unsafe int madvise(byte* addr, nuint length, int advice);

    // POSIX Compatible
    [DllImport("libc.so", EntryPoint = "madvise")]
    private static extern unsafe int madvise_android(byte* addr, nuint length, int advice);

    // OSX
    [DllImport("libSystem", EntryPoint = "madvise")]
    private static extern unsafe int madvise_libSystem(byte* addr, nuint length, int advice);

    // Windows-Like
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryRangeEntry
    {
        public nint VirtualAddress;
        public nuint NumberOfBytes;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern unsafe bool PrefetchVirtualMemory(
        IntPtr hProcess,
        UIntPtr numberOfEntries,
        MemoryRangeEntry* memoryRanges,
        uint flags);
    #endregion
}
