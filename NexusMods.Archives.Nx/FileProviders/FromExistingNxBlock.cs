using System.Diagnostics.CodeAnalysis;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.FileProviders.FileData;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Structs.Blocks;
using NexusMods.Archives.Nx.Utilities;
using static NexusMods.Archives.Nx.Utilities.Polyfills;

namespace NexusMods.Archives.Nx.FileProviders;

/// <summary>
///     This provider allows you to read a file from an existing Nx block.
///
///     This is used when you only want some files from an exising block in a
///     foreign Nx archive.
/// </summary>
/// <remarks>
///     This class has 'special' behaviour, and thus is internal.
///
///     In order to work efficiently and release memory as soon as possible,
///     it relies on the implementation detail that <see cref="GetFileData"/>
///     is only called once on this provider during packing from any implementation
///     of <see cref="IBlock{T}"/>. We then do reference counting around that.
///
///     This behaviour is asserted via the tests (see <see cref="LazyRefCounterDecompressedNxBlock.InternalGetRefCount"/>).
///     If it does not hold true, the test will fail.
/// </remarks>
internal class FromExistingNxBlock : IFileDataProvider
{
    /// <summary>
    ///     Contains the decompressed chunk of data.
    /// </summary>
    private LazyRefCounterDecompressedNxBlock LazyRefCounterDecompressedNxBlock { get; init; }

    /// <summary>
    ///     Size of the file in the decompressed block.
    /// </summary>
    private ulong FileSize { get; init; }

    /// <summary>
    ///     This is the offset into the decompressed Nx block where the file starts.
    /// </summary>
    private int DecompressedBlockOffset { get; init; }

    #if DEBUG
    /// <summary>
    ///     See class remarks for more info.
    /// </summary>
    private bool _hasCalledGetData;
    #endif

    /// <summary/>
    /// <param name="block">Shared block instance.</param>
    /// <param name="entry">The file entry which belongs to this block.</param>
    public FromExistingNxBlock(LazyRefCounterDecompressedNxBlock block, FileEntry entry)
    {
        LazyRefCounterDecompressedNxBlock = block;
        DecompressedBlockOffset = entry.DecompressedBlockOffset;
        FileSize = entry.DecompressedSize;
        block.Acquire();
    }

    /// <inheritdoc />
    public IFileData GetFileData(ulong start, ulong length)
    {
        #if DEBUG
        if (_hasCalledGetData)
            throw new Exception("Repeated call to GetData. This is not allowed.");

        _hasCalledGetData = true;
        #endif

        return new DecompressedNxBlockFileData(LazyRefCounterDecompressedNxBlock, DecompressedBlockOffset, FileSize);
    }
}

/// <summary>
///     This represents a block of data from an existing Nx archive; with
///     custom reference counting.
///
///     The behaviour is as follows.
///
///     - Every Item referencing this block will increment the reference counter.
///         - To do this, call <see cref="Acquire"/>.
///         - When the reference counter reaches 0, the memory is released.
///     - To get the data of the block, you call <see cref="GetData"/>.
///         - If this data is not yet decompressed.
///
///     When the reference count reaches 0, the memory is released.
///     The idea is that <see cref="FromExistingNxBlock"/> will hold a reference to this block.
///     It will then increment the reference count when it's used.
/// </summary>
internal unsafe class LazyRefCounterDecompressedNxBlock : IDisposable
{
    /// <summary>
    ///     The raw data of the decompressed chunk.
    /// </summary>
    private volatile void* _data;

    /// <summary>
    ///     This is a reference counter.
    ///     When this counter reaches 0, the memory is released.
    /// </summary>
    private int _refCount;

    /// <summary>
    ///     Provides access to the original Nx archive.
    /// </summary>
    private readonly IFileDataProvider _sourceNxDataProvider;

    /// <summary>
    ///     Number of bytes that need decompressing for all files that will be extracted
    ///     from this block.
    /// </summary>
    private ulong _numBytesToDecompress;

    /// <summary>
    ///     Offset of the block in original Nx archive (via <see cref="_sourceNxDataProvider"/>).
    /// </summary>
    private readonly ulong _blockOffset;

    /// <summary>
    ///     Length of the block in the original Nx archive (via <see cref="_sourceNxDataProvider"/>).
    /// </summary>
    private readonly ulong _compressedBlockLength;

    /// <summary>
    ///     Compression used with the original Nx archive (via <see cref="_sourceNxDataProvider"/>).
    /// </summary>
    private readonly CompressionPreference _compression;

    /// <summary/>
    /// <param name="sourceNxDataProvider">This provides raw access to the Nx file.</param>
    /// <param name="blockOffset">Byte offset of the block in the <paramref name="sourceNxDataProvider"/>.</param>
    /// <param name="compressedBlockLength">Length of the block at <paramref name="sourceNxDataProvider"/>.</param>
    /// <param name="compression">Compression used by the block at <paramref name="sourceNxDataProvider"/>.</param>
    /// <remarks>
    ///     How do we know length of decompressed block?
    ///     Header could specify max block size.
    ///     Compressed data could specify in prefix.
    /// </remarks>
    public LazyRefCounterDecompressedNxBlock(IFileDataProvider sourceNxDataProvider, ulong blockOffset, ulong compressedBlockLength, CompressionPreference compression)
    {
        _sourceNxDataProvider = sourceNxDataProvider;
        _blockOffset = blockOffset;
        _compressedBlockLength = compressedBlockLength;
        _compression = compression;
    }

    /// <summary>
    ///     This increments the reference count by 1.
    /// </summary>
    public void Acquire() => Interlocked.Increment(ref _refCount);

    /// <summary>
    ///     Releases the shared decompressed block.
    ///     Call this from an <see cref="IFileData"/> when it's done exposing
    ///     the data behind this block.
    /// </summary>
    public void Release()
    {
        if (Interlocked.Decrement(ref _refCount) == 0)
            Dispose();

#if DEBUG
        if (_refCount < 0)
            throw new InvalidOperationException("Reference count is negative.");
#endif
    }

    /// <summary>
    ///     Updates the number of bytes that need decompressing
    ///     in this block based on the file entry.
    ///     (The file needs to belong to this block.)
    /// </summary>
    /// <param name="entry"></param>
    public void ConsiderFile(FileEntry entry)
    {
        var maxOffset = (ulong)entry.DecompressedBlockOffset + entry.DecompressedSize;
        if (maxOffset > _numBytesToDecompress)
            _numBytesToDecompress = maxOffset;
    }

    /// <summary>
    ///     For test use only.
    /// </summary>
    internal int InternalGetRefCount() => _refCount;

    /// <summary>
    ///     This retrieves the raw data and length of the block.
    ///     If the data is not yet available,
    /// </summary>
    /// <returns>A pointer to the current data.</returns>
    [SuppressMessage("ReSharper", "ReadAccessInDoubleCheckLocking")] // our field is volatile.
    public void* GetData()
    {
        // If the data is already decompressed, we don't need to do it again.
        if (_data != null)
            return _data;

        // Not much contention expected so full lock is ok, no need for extra field and 'cmpxchg' here.
        lock (this)
        {
            // Another thread might have already initialized in lock.
            if (_data != null)
                return _data;

            _data = AllocNativeMemory((nuint)_numBytesToDecompress);
            using var rawBlockData = _sourceNxDataProvider.GetFileData(_blockOffset, _compressedBlockLength);
            Compression.Decompress(_compression, rawBlockData.Data, (int)rawBlockData.DataLength, (byte*)_data, (int)_numBytesToDecompress);
            return _data;
        }
    }

    /// <inheritdoc />
    ~LazyRefCounterDecompressedNxBlock() => ReleaseUnmanagedResources();

    /// <inheritdoc />
    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        if (_data == null)
            return;

#if DEBUG
        if (Interlocked.CompareExchange(ref _refCount, 0, 0) != 0)
            throw new InvalidOperationException("Memory is being released while there are still references to it.");
#endif

        FreeNativeMemory(_data);
        _data = null;
    }
}
