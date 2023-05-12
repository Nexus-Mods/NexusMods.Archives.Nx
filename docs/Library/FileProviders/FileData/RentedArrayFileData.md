# RentedArrayFileData

!!! info "The `RentedArrayFileData` class is an implementation of `IFileData` backed up by an `ArrayPool` rental."

## Properties

- `Data`: The pointer to the start of the data.
- `DataLength`: The length of the data.

## Constructors

- `RentedArrayFileData(ArrayRentalSlice data)`: Creates file data backed by a rented array.

## Destructor

- `~RentedArrayFileData()`: Disposes the object, freeing the pinned array.

## Methods

- `Dispose()`: Frees the pinned array.

## Usage

```csharp
var slice = new ArrayRentalSlice(new ArrayRental<byte>(666), 666);
using var fileData = new RentedArrayFileData(slice);

// Do something with fileData...
```

In this example, the `RentedArrayFileData` wraps a slice of a rented array. The resulting `RentedArrayFileData` object 
provides a pointer to the start of the data and the length of the data.