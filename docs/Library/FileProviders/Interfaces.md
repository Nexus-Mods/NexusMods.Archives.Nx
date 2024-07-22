# Interfaces

These interfaces define the structure for providing access to underlying file data and creating instances of file data,
particularly useful for unpacking purposes.

## IFileData Interface

!!! info "The `IFileData` interface provides access to underlying file data. This is particularly useful for read operations where the entire file is not yet available, such as over a network; you should stall until you can provide enough data."

### Properties

- `Data`: A pointer to the data of the underlying item.
- `DataLength`: The length of the underlying data.

## IFileDataProvider Interface

!!! info "The `IFileDataProvider` interface creates `IFileData` instances for the purposes of getting data for the archiving operation."

### Methods

- `GetFileData(ulong start, ulong length)`: Gets the file data behind this provider.

## IOutputDataProvider Interface

!!! info "The `IOutputDataProvider` interface creates `IFileData` instances which allow the user to output information for unpacking purposes. Note that items are disposed upon successful write to target, not explicitly by the user."

### Properties

- `RelativePath`: The relative path to the output location.
- `Entry`: The entry this provider is for.

### Methods

- `GetFileData(ulong start, ulong length)`: Gets the output data behind this provider. Returns an individual `IFileData` buffer to write decompressed data to. Make sure to dispose, for example with a 'using' statement.
