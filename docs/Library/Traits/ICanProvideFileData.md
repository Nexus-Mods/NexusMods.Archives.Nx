# ICanProvideFileData

!!! info "`ICanProvideFileData` is a trait for items which can provide data to be compressed"

This trait is used inside the packing code (notably in `PackerFile`) to provide the bytes to be compressed.

## Properties

### FileDataProvider

```csharp
IFileDataProvider FileDataProvider { get; }
```

## Usage

```csharp
public class MyFileDataClass : ICanProvideFileData
{
    /// <inheritdoc />
    public required IFileDataProvider FileDataProvider { get; init; }
}

// Get the data.
using var data = MyFileDataClass.FileDataProvider.GetFileData(StartOffset, (uint)ChunkSize);
```

In this example, `MyFileDataClass` implements the `ICanProvideFileData` interface.  
`MyFileDataClass` can then be used in methods constrained with `where T : ICanProvideFileData`.
