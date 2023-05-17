using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     A tuple of array pools for the packer.
/// </summary>
internal readonly struct PackerArrayPools : IDisposable
{
    /// <summary>
    ///     Designated pool for blocks.
    /// </summary>
    public PackerArrayPool BlockPool { get; }

    /// <summary>
    ///     Designated pool for chunks.
    /// </summary>
    public PackerArrayPool ChunkPool { get; } = null!;

    /// <summary>
    ///     Initializes the tuple of pools.
    /// </summary>
    /// <param name="concurrentWorkers">Number of max concurrent rentals.</param>
    /// <param name="blockSize">Block size used by the pool.</param>
    /// <param name="chunkSize">Chunk size used by the pool, if any chunks are to be made.</param>
    public PackerArrayPools(int concurrentWorkers, int blockSize, int? chunkSize)
    {
        // We do *2 here because solid blocks require one for compressed, and one for decompressed.
        BlockPool = new PackerArrayPool(concurrentWorkers * 2, blockSize);
        if (chunkSize != null)
            ChunkPool = new PackerArrayPool(concurrentWorkers, chunkSize.GetValueOrDefault());
    }

    public void Dispose()
    {
        BlockPool?.Dispose();
        ChunkPool?.Dispose();
    }
}

/// <summary>
///     Implementation of Array Pooling for the packing operations.
///     Uses shared <see cref="ArrayPool{T}" /> for smaller allocations, and custom one for bigger ones.
/// </summary>
internal class PackerArrayPool : IDisposable
{
    /// <summary>
    ///     Maximum size that can be rented from the shared pool.
    /// </summary>
    private const int SharedPoolMaxSize = 1048576;

    /// <summary>
    ///     Checks if internal arrays have been allocated.
    /// </summary>
    public bool HasArrays => _arrays != null;

    private readonly int[]? _arraysTaken;
    private byte[][]? _arrays;

    /// <summary />
    /// <param name="concurrentWorkers">Number of max concurrent rentals.</param>
    /// <param name="blockSize">Block/chunk size used by the pool.</param>
    public PackerArrayPool(int concurrentWorkers, int blockSize)
    {
        // Note: We increase the block size based on max output possible so the compressors can stop checking length and compress faster.
        var maxSize = Compression.MaxAllocForCompressSize(blockSize);
        if (maxSize < SharedPoolMaxSize)
            return;

        _arrays = new byte[concurrentWorkers][];
        _arraysTaken = new int[concurrentWorkers];
        for (var x = 0; x < concurrentWorkers; x++)
            _arrays[x] = Polyfills.AllocateUninitializedArray<byte>(maxSize, true);
    }

    public PackerPoolRental Rent(int numBytes)
    {
        var data = RentInternal(numBytes, out var index);
        return new PackerPoolRental
        {
            Array = data,
            Owner = this,
            Length = numBytes,
            ArrayIndex = index
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte[] RentInternal(int numBytes, out int arrayIndex)
    {
        arrayIndex = -1;
        if (numBytes <= SharedPoolMaxSize)
            return ArrayPool<byte>.Shared.Rent(numBytes);

        Debug.Assert(_arrays != null);
        Debug.Assert(_arrays![0].Length >= numBytes);

        for (var x = 0; x < _arraysTaken!.Length; x++)
        {
            // Use CompareExchange to make operation atomic, and thus thread safe.
            var oldValue = Interlocked.CompareExchange(ref _arraysTaken[x], 1, 0);

            // Hot path, don't invert if.
            // ReSharper disable once InvertIf
            if (oldValue == 0)
            {
                arrayIndex = x;
                return _arrays![arrayIndex];
            }
        }

        ThrowHelpers.ThrowPackerPoolOutOfItems();
        return default!;
    }

    public void Return(PackerPoolRental packerPoolRental)
    {
        if (packerPoolRental.Length <= SharedPoolMaxSize)
        {
            ArrayPool<byte>.Shared.Return(packerPoolRental.Array);
            return;
        }

        // Use CompareExchange to make operation atomic, and thus thread safe.
        Interlocked.CompareExchange(ref _arraysTaken![packerPoolRental.ArrayIndex], 0, 1);
    }

    /// <inheritdoc />
    public void Dispose() => _arrays = null!;
}

internal readonly struct PackerPoolRental : IDisposable
{
    /// <summary>
    ///     The underlying array for this rental.
    /// </summary>
    public required byte[] Array { get; init; }

    /// <summary>
    ///     The packer pool which created this rental.
    /// </summary>
    public required PackerArrayPool Owner { get; init; }

    /// <summary>
    ///     Length of the rented data.
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    ///     Index of array in the owner pool.
    ///     This can be -1 if the array was rented from the shared pool.
    /// </summary>
    public int ArrayIndex { get; init; }

    /// <summary>
    ///     Returns the span for given rented array.
    /// </summary>
    public Span<byte> Span => Array.AsSpan(0, Length);

    /// <inheritdoc />
    public void Dispose() => Owner.Return(this);
}
