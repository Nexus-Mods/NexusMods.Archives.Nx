# FromDirectoryDataProvider

!!! info "The `FromDirectoryDataProvider` provides file data from a file in a given directory."

## Properties

- `Directory`: The directory from which the data will be fetched.
- `RelativePath`: Relative path to the directory.

## Methods

### GetFileData

!!! info "Retrieves file data from the directory based on the given start index and length."

```csharp
public IFileData GetFileData(long start, uint length)
```

## Usage

The `FromDirectoryDataProvider` is used to fetch file data from a directory. The directory and relative path are stored separately to save memory. When `GetFileData` is called, the directory and relative path are combined temporarily to fetch the data. Here's an example:

```csharp
var dataProvider = new FromDirectoryDataProvider
{
    Directory = "/path/to/directory",
    RelativePath = "relative/path/to/file"
};

using var fileData = dataProvider.GetFileData(0, 1024);

// Use fileData...
```

In this example, a `FromDirectoryDataProvider` is created and used to fetch the first 1024 bytes of a file located in 
the directory `/path/to/directory` and the relative path `relative/path/to/file`. The resulting `IFileData` object 
can then be used as needed.