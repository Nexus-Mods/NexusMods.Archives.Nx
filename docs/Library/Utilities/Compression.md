# Compression

!!! info "Utility methods for compressing and decompressing raw data"

This class provides the glue code for compressing and decompressing data using
the supported compression methods.

These methods are useful for low level operations, such as providing 3rd party
abstractions like Streams over Nx data.

## Methods

!!! tip "This is a static class."

### MaxAllocForCompressSize

```csharp
public static int MaxAllocForCompressSize(int sourceLength)
```

Determines the maximum memory needed to allocate to compress data with any method.

- Parameters:
    - `sourceLength`: Number of bytes in the source data.
- Returns: The maximum number of bytes that might be needed for compressed data.

### AllocForCompressSize

```csharp
public static int AllocForCompressSize(CompressionPreference method, int sourceLength)
```

Determines the memory needed to allocate to compress data with a specified method.

- Parameters:
    - `method`: The compression method to be used.
    - `sourceLength`: Number of bytes in the source data.
- Returns: The number of bytes needed for compression with the specified method.

### Compress

```csharp
public static unsafe int Compress(CompressionPreference method, int level, byte* source, int sourceLength, byte* destination, int destinationLength, out bool defaultedToCopy)
```

Compresses data with a specific method.

- Parameters:
    - `method`: The compression method to use.
    - `level`: The compression level.
    - `source`: Pointer to the source data.
    - `sourceLength`: Length of the source in bytes.
    - `destination`: Pointer to the destination buffer.
    - `destinationLength`: Length of the destination buffer.
    - `defaultedToCopy`: Set to true if data was uncompressible and default compression (copy) was used instead.
- Returns: The number of bytes written to the destination buffer.

### Decompress

```csharp
public static unsafe void Decompress(CompressionPreference method, byte* source, int sourceLength, byte* destination, int destinationLength)
```

Decompresses data with a specific method.

- Parameters:
    - `method`: The compression method to use.
    - `source`: Pointer to the compressed data.
    - `sourceLength`: Length of the compressed data in bytes.
    - `destination`: Pointer to the destination buffer for decompressed data.
    - `destinationLength`: Length of the destination buffer.

## Usage Example

Here's a simple example of how to use the Compression utility to compress and decompress data:

```csharp
// Compress data
var sourceData = GetSomeData();
int maxCompressedSize = Compression.MaxAllocForCompressSize(sourceData.Length);
var compressedData = GC.AllocateUninitializedArray<byte>(maxCompressedSize);

fixed (byte* sourcePtr = sourceData)
fixed (byte* destPtr = compressedData)
{
    // Compress the data.
    int compressedSize = Compression.Compress(CompressionPreference.ZStandard, 16, sourcePtr, sourceData.Length, destPtr, maxCompressedSize, out bool defaultedToCopy);

    // Decompress data
    var decompressedData = GC.AllocateUninitializedArray<byte>(sourceData.Length);
    fixed (byte* decompressPtr = decompressedData)
    {
        Compression.Decompress(CompressionPreference.ZStandard, destPtr, compressedSize, decompressPtr, sourceData.Length);
    }
}
```

This example demonstrates compressing data using ZStandard compression,
then decompressing it back to its original form using the given APIs.

For a real life, advanced low level example, consider checking out Streaming Decompression
from [NxFileStore in NexusMods.App][advanced-compress-example] which is derived
from the library's [NxUnpacker][nx-unpacker].

[advanced-compress-example]: https://github.com/Nexus-Mods/NexusMods.App/blob/cdfcc2d47e0e2c572572f1f0a7ed0bd452e7bd44/src/NexusMods.DataModel/NxFileStore.cs#L292
[nx-unpacker]: https://github.com/Nexus-Mods/NexusMods.Archives.Nx/blob/97b4955804660e7b1375ab585360efa036350541/NexusMods.Archives.Nx/Packing/NxUnpacker.cs#L173
