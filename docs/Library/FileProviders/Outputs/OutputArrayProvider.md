# OutputArrayProvider Class

!!! info "The `OutputArrayProvider` is an output data provider that writes data to an array. It is used for extracting archived content to a byte array."

## Properties

- `Data`: The byte array held by this provider.
- `RelativePath`: The relative path of the file.
- `Entry`: The file entry from the archive.

## Constructors

- `OutputArrayProvider(string relativePath, FileEntry entry)`: Initializes an `OutputArrayProvider` with the specified relative path and file entry.

## Methods

### GetFileData

!!! info "Retrieves the file data from the array based on the given start index and length."

```csharp
public IFileData GetFileData(ulong start, ulong length)
```

### Dispose

!!! info "Disposes of the output array provider and releases any resources associated with it."

```csharp
public void Dispose()
```

## Examples

```csharp
// Assuming unpacker is an instance of NxUnpacker and files is a collection of FileEntry instances from NxUnpacker
var entry = files[x];
var relPath = unpacker.GetFilePath(entry.FilePathIndex);
var provider = new OutputArrayProvider(relPath, entry);
```
