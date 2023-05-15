# FileFinder Class

!!! info "A utility class for creating `PackerFile(s)` which can be fed into the packing APIs such as `NxPacker.Pack`."

## Methods

!!! tip "This is a static class."

### GetFiles

!!! info "Retrieves all packable files from a directory."

```csharp
List<PackerFile> GetFiles(string directoryPath, SearchOption searchOption)
```

The search option specifies whether the search operation should include all subdirectories or only the current directory.

!!! note
    
    Depending on the .NET version, the implementation of this method varies. For .NET Standard 2.1 or greater, this method is
    considerably faster with the use of newer APIs.

### GetFiles (EnumerationOptions)

!!! info "For .NET Standard 2.1 or greater, the following API is available."

```csharp
List<PackerFile> GetFiles(string directoryPath, EnumerationOptions options)
```

This API accepts some additional enumeration options to use when searching for files.

## Examples

### Creating Packable Files

```csharp
var files = FileFinder.GetFiles("C:/Mods/SomeCoolMod", SearchOption.AllDirectories);
foreach (var file in files)
    Console.WriteLine($"Found file: {file.RelativePath}, Size: {file.FileSize}");
```

In this example, the `GetFiles` method is used to find all files in the specified directory and its subdirectories.  
The result of this can be printed to console, or sent straight to the mid-level API packer.  
