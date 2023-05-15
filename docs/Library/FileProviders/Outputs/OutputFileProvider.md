# OutputFileProvider Class

!!! info "The `OutputFileProvider` is an output data provider that writes data to a file. It is used for extracting archived content to a file on disk."

## Properties

- `RelativePath`: The relative path of the file.
- `Entry`: The file entry from the archive.

## Constructors

- `OutputFileProvider(string outputFolder, string relativePath, FileEntry entry)`: Initializes an `OutputFileProvider` with the specified output folder, relative path, and file entry.

## Methods

### GetFileData

!!! info "Retrieves the file data from the array based on the given start index and length."

```csharp
public IFileData GetFileData(long start, uint length)
```

### Dispose

!!! info "Disposes of the output file provider and releases any resources associated with it."

```csharp
public void Dispose()
```

## Usage

The `OutputFileProvider` class is used to output extracted file data to a file. Here's an example of how to use it:

```csharp
// Assuming unpacker is an instance of NxUnpacker and files is a collection of FileEntry instances from NxUnpacker
var entry = files[x];
string outputFolder = "C:\\OutputFolder";
string relativePath = "folder\\file.txt";
var provider = new OutputFileProvider(outputFolder, relativePath, entry);
```