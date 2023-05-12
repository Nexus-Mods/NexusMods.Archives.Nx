# FromStreamProvider

!!! info "The `FromStreamProvider` class is a file data provider that provides file data from a stream."

!!! warning "Underlying stream must support seeking."

## Properties

- `Stream`: The stream associated with this provider.
- `StreamStart`: The start position of the stream.

## Constructors

- `FromStreamProvider(stream)`: Creates a `FromStreamProvider` instance with the specified stream.

## Methods

### GetFileData

!!! info "Retrieves file data from the stream based on the given start index and length."

```csharp
public IFileData GetFileData(long start, uint length)
```

## Usage

```csharp
var provider = new FromStreamProvider(GetFileStream());

// Get file data from the stream
using var fileData = provider.GetFileData(0, 1024);

// Use fileData...
```

In this example, a stream is obtained from a source, and a `FromStreamProvider` is created with the stream. 
The `GetFileData` method is then used to retrieve file data from the stream starting at index 0 with a length of 1024. 
The resulting `IFileData` object can be used as needed.