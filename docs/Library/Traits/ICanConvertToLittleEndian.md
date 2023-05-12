# ICanConvertToLittleEndian

!!! info "The `ICanConvertToLittleEndian` trait is for structures that can convert to Little Endian format."

It provides the `ReverseEndianIfNeeded` method for reversing the endian of the data on a big endian machine, if required.

## Methods

### ReverseEndianIfNeeded

```csharp
public void ReverseEndianIfNeeded();
```

Reverses the endian of the data if needed. Only call this method once, or endian will be reversed again.  
This is intended to be used in cases where you receive a structure from an external source, e.g. reading an
.nx file from disk or transferred over the network.

!!! note "The implementing methods of this interface are defined as follows"

```csharp
if (BitConverter.IsLittleEndian)
    return;

ReverseEndian(); 
```

The `BitConverter.IsLittleEndian` is evaluated at compile-time (JIT-time) and is a no-op on Little Endian machines.

## Usage

```csharp
public struct MyStruct : ICanConvertToLittleEndian
{
    public int MyValue;
    
    public void ReverseEndianIfNeeded()
    {
        if (BitConverter.IsLittleEndian)
            return;

        MyValue = BinaryPrimitives.ReverseEndianness(MyValue);
    }
}
```

In the above example, `MyStruct` implements the `ICanConvertToLittleEndian` interface.  
The `ReverseEndianIfNeeded` method checks if the system is Little Endian. If not, it reverses the endian of `MyValue`.  
