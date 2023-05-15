# IHasFileSize

!!! info "The `IHasFileSize` trait allows items to specify a file size in bytes."

## Properties

### FileSize

```csharp
long FileSize { get; }
```

This property gets the size of the item in bytes.

## Usage

The `IHasFileSize` interface is used to indicate that an item can specify its file size. Implementing this interface in a class allows the class to expose the `FileSize` property.

```csharp
public class MyFileSizeItem : IHasFileSize
{
    /// <inheritdoc />
    public required long FileSize { get; init; }
}
```

In this example, `MyFileSizeItem` implements the `IHasFileSize` interface.  
`MyFileSizeItem` can then be used in methods constrained with `where T : IHasFileSize`.
