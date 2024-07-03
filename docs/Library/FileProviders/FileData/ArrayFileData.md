# ArrayFileData

!!! info "The `ArrayFileData` class is an implementation of `IFileData` backed up by a pinned array."

## Properties

- `Data`: The pointer to the start of the data.
- `DataLength`: The length of the data.

## Constructors

- `ArrayFileData(byte[] data, long start, uint length)`: Creates file data backed by an array.

## Destructor

- `~ArrayFileData()`: Disposes the object, freeing the pinned handle.

## Methods

- `Dispose()`: Frees the pinned handle.
