# StringPool

!!! tip "[The structure and purpose of the StringPool is detailed in the specification.](../../Specification/Table-Of-Contents.md#string-pool)"

## Constants

- `MaxCompressedSize`: The maximum allowed size of the compressed string pool.
- `DefaultCompressionLevel`: The default compression level used for the `Zstandard` compression algorithm.

## Methods

### Pack

This method packs a list of items into the string pool.

```csharp
public static unsafe ArrayRentalSlice Pack<T>(Span<T> items) where T : IHasRelativePath
```

!!! info "This method takes a `Span<T>` of items, where `T` is a type implementing the `IHasRelativePath` interface. The items are sorted lexicographically and packed into the string pool."

!!! warning "This sorts the span directly, i.e. order of items in the span will be different after calling the method."

### Unpack

These methods decompress a previously packed string pool, returning an array of the original strings.

```csharp
public static unsafe string[] Unpack(byte* poolPtr, int compressedDataSize)
public static unsafe string[] Unpack(byte* poolPtr, int compressedDataSize, int fileCountHint)
public static unsafe string[] Unpack(Span<byte> poolSpan)
public static unsafe string[] Unpack(Span<byte> poolSpan, int fileCountHint)
```

!!! info "These methods take a pointer to the compressed string pool and the size of the compressed data. They return an array of the original strings. Optionally, you can provide a hint for the number of files contained in the string pool."

## Examples

### Packing Strings into the String Pool

```csharp
// Create a list of items implementing IHasRelativePath interface.
Span<IHasRelativePath> items = new IHasRelativePath[]
{
    new Item { RelativePath = "item1" },
    new Item { RelativePath = "item2" },
    // ...
};

// Pack the items into the string pool.
using var packedData = StringPool.Pack(items);
```

### Unpacking Strings from the String Pool

```csharp
// Assume we have a byte pointer to the compressed string pool and its size.
byte* poolPtr = ...;
int compressedDataSize = ...;

// Unpack the strings from the string pool.
var strings = StringPool.Unpack(poolPtr, compressedDataSize);

// Now, 'strings' contains the original strings.
```