# FromFilePathProvider

!!! info "The `FromFilePathProvider` class is a file data provider that provides data from a specified file path."

This is basically [FromDirectoryDataProvider](FromDirectoryDataProvider.md) but with
the full path to the file instead of the directory and file name.

## Properties

- `FilePath`: The full path to the file from which the data will be fetched.

## Methods

### GetFileData

```csharp
public IFileData GetFileData(ulong start, ulong length)
```

Retrieves file data from the specified file path based on the given start index and length.

- Parameters:
    - `start`: The starting offset in the file.
    - `length`: The number of bytes to read.
- Returns: 
    - An `IFileData` object representing the requested file data.

## Usage

```csharp
var provider = new FromFilePathProvider
{
    FilePath = "/path/to/your/file.txt"
};

// Get file data from the file
using var fileData = provider.GetFileData(0, 1024);

// Use fileData...
```

In this example, a `FromFilePathProvider` is created with a specified file path.
The `GetFileData` method is then used to retrieve file data from the file starting
at offset 0 with a length of 1024 bytes.

The resulting `IFileData` object can be used as needed.

!!! note "The `FromFilePathProvider` uses memory-mapped files internally for efficient data access."

!!! warning "Ensure that the specified file exists and is accessible before using this provider."
