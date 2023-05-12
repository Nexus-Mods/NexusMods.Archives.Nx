# IHasCompressionPreference

!!! info "The `IHasCompressionPreference` trait allows an item to declare the compression algorithm it wants to be processed with."

## Properties

### CompressionPreference

```csharp
CompressionPreference CompressionPreference { get; }
```

This property gets the preferred algorithm to compress the item with. The `CompressionPreference` enum specifies the available compression algorithms.

The `CompressionPreference` enum defines the following values (at time of writing):  

- `NoPreference`: No preference is specified.  
- `Copy`: Do not compress at all, copy data verbatim.  
- `ZStandard`: Compress with ZStandard.  
- `Lz4`: Compress with LZ4.  

## Usage

```csharp
public class MyCompressibleItem : IHasCompressionPreference
{
    /// <inheritdoc />
    public required CompressionPreference CompressionPreference { get; init; }
}

// Set the preference.
MyCompressibleItem.CompressionPreference = CompressionPreference.Lz4;
```

In this example, `MyCompressibleItem` implements the `IHasCompressionPreference` interface.  
`MyCompressibleItem` can then be used in methods constrained with `where T : IHasCompressionPreference`.
