# MemoryMappedFileData

!!! info "The `MemoryMappedFileData` class is an implementation of `IFileData` backed up by memory-mapped files. It provides an efficient way to work with large data files."

## Properties

- `Data`: The pointer to the start of the data.
- `DataLength`: The length of the data.

## Constructors

- `MemoryMappedFileData(string filePath, ulong start, ulong length)`: Creates file data backed by a memory mapped file from a given file path.
- `MemoryMappedFileData(FileStream stream, ulong start, ulong length)`: Creates file data backed by a memory mapped file from a given file stream.

## Destructor

- `~MemoryMappedFileData()`: Disposes the object, freeing the memory-mapped file and its view.

## Methods

- `Dispose()`: Frees the memory-mapped file and its view.

## Usage

This class provides an efficient way to handle large data files. It uses memory-mapped files to access parts of a file
as if they were in memory. Here's a brief example:

```csharp
using var fileData = new MemoryMappedFileData("path/to/file", 0, 100);

// Do something with fileData...
```

In this example, the `MemoryMappedFileData` maps the first 100 bytes of the file at the given path into memory.
The resulting `MemoryMappedFileData` object provides a pointer to the start of the data and the length of the data.
