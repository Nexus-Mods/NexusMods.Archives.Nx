# FromArrayProvider

These classes are part of a system that provides and manages data from an array for files.

## FromArrayProvider

!!! info "The `FromArrayProvider` class is a file data provider that provides info from an array."

### Properties

- `Data`: The array held by this provider.

### Methods

- `GetFileData(ulong start, ulong length)`: Returns file data backed by the array it holds.

## Usage

These classes work together to provide and manage file data from an array. Here's a brief example:

```csharp
var provider = new FromArrayProvider
{
    Data = new byte[] { 1, 2, 3, 4, 5 }
};

using var fileData = provider.GetFileData(1, 2);

// Do something with fileData...
```
