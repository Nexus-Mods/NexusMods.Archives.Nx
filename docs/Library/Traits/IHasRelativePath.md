# IHasRelativePath

!!! info "The `IHasRelativePath` trait allows items to specify a relative path to the file from the archive or folder root."

## Properties

### RelativePath

```csharp
string RelativePath { get; }
```

This property gets the relative path to the file from the archive or folder root.

## Usage

The `IHasRelativePath` interface is used to indicate that an item contains a file path. 
Implementing this interface in a class allows the class to expose the `RelativePath` property.

```csharp
public class MyFileItem : IHasRelativePath
{
    /// <inheritdoc />
    public string RelativePath { get; set; }
}
```

In this example, `MyFileItem` implements the `IHasRelativePath` interface.  
`MyFileItem` can then be used in methods constrained with `where T : IHasRelativePath`.