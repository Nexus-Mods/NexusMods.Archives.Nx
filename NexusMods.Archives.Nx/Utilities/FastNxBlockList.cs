#if DEBUG
using System.Diagnostics;
#endif
using System.Runtime.CompilerServices;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     This is a container which stores a pre-determined number of groups (blocks),
///     and a pre-determined max number of items.
/// </summary>
/// <typeparam name="T">The type of elements in the list. Must be an unmanaged type.</typeparam>
internal unsafe struct BlockList<T> : IDisposable where T : unmanaged
{
    private readonly void* _data;
    private T* _currentItem;
    private readonly int _blockCount;
#if DEBUG
    private readonly T* _maxItem;
#endif

    private FixedSizeList<T>* Blocks => (FixedSizeList<T>*)_data;

    /// <summary>
    ///     Initializes a new instance of the CustomList struct.
    /// </summary>
    /// <param name="blockCount">Number of blocks (keys) in the list.</param>
    /// <param name="maxNumItems">Complete/max number of items in the list.</param>
    public BlockList(int blockCount, int maxNumItems)
    {
        // Calculate total memory needed
        var blockSectionSize = sizeof(FixedSizeList<T>) * blockCount;
        var itemSectionSize = sizeof(T) * maxNumItems;
        _data = Polyfills.AllocNativeMemory((UIntPtr)(blockSectionSize + itemSectionSize));
        _currentItem = (T*)((byte*)_data + blockSectionSize);
        _blockCount = blockCount;

#if DEBUG
        _maxItem = _currentItem + maxNumItems;
#endif

        // Zero-fill the block section
        Unsafe.InitBlock(_data, 0, (uint)blockSectionSize);
    }

    /// <summary>
    ///     Gets or creates a FixedSizeList for the specified block index.
    /// </summary>
    /// <param name="blockIndex">Index of the block to obtain.</param>
    /// <param name="itemCount">Maximum number of possible items in the block (in case block needs to be created).</param>
    /// <returns>The existing list, if already exists, or a new list if created.</returns>
    public FixedSizeList<T>* GetOrCreateList(int blockIndex, int itemCount)
    {
#if DEBUG
        Debug.Assert((uint)blockIndex < (uint)_blockCount, "Block index out of range.");
#endif

        var block = &Blocks[blockIndex];
        if (block->IsValid)
            return block;

#if DEBUG
        Debug.Assert(_currentItem + itemCount <= _maxItem, "Not enough space to allocate new block.");
#endif

        // Allocate this block
        block->Initialize(_currentItem);
#if DEBUG
        block->SetCapacity(itemCount);
#endif

        _currentItem += itemCount;
        return block;
    }

    /// <summary>
    ///     Returns a span containing all the blocks in this BlockList.
    /// </summary>
    /// <returns>A span of all FixedSizeList blocks.</returns>
    public Span<FixedSizeList<T>> GetAllBlocks() => new(Blocks, _blockCount);

    /// <inheritdoc />
    public void Dispose() => Polyfills.FreeNativeMemory(_data);
}

/// <summary>
/// Represents a custom list structure that works with preallocated memory.
/// </summary>
/// <typeparam name="T">The type of elements in the list. Must be an unmanaged type.</typeparam>
internal unsafe struct FixedSizeList<T> where T : unmanaged
{
    private T* _data;
    private int _count;
#if DEBUG
    private int _capacity;
#endif

    /// <summary>
    ///     Initializes the FixedSizeList with the given data pointer.
    /// </summary>
    /// <param name="data">Pointer to the preallocated memory.</param>
    public void Initialize(T* data)
    {
        _data = data;
        _count = 0;
    }

#if DEBUG
    /// <summary>
    ///     Sets the capacity of the list. This method is only available in debug builds.
    /// </summary>
    /// <param name="capacity">The maximum number of items this list can hold.</param>
    public void SetCapacity(int capacity)
    {
        _capacity = capacity;
    }
#endif

    /// <summary>
    ///     Returns true if the list has been initialized with valid data.
    /// </summary>
    public bool IsValid => _data != null;

    /// <summary>
    ///     Adds an item to the end of the list.
    /// </summary>
    /// <param name="item">The item to add to the list.</param>
    /// <remarks>
    ///     This method does not perform bounds checking. Ensure that the preallocated
    ///     memory has sufficient space to accommodate the new item.
    /// </remarks>
    public void Push(T item)
    {
#if DEBUG
        Debug.Assert(_count < _capacity, "Capacity exceeded in FixedSizeList.");
#endif
        _data[_count++] = item;
    }

    /// <summary>
    ///     Returns a span representing the contents of the list.
    /// </summary>
    /// <returns>A span containing the elements of the list.</returns>
    public Span<T> AsSpan() => new(_data, _count);

    /// <summary>
    ///     Gets the number of elements contained in the list.
    /// </summary>
    public int Count => _count;
}
