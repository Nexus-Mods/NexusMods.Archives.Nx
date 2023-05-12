# LittleEndianReader

!!! info "The `LittleEndianReader` is a utility for reading data from a pointer in Little Endian format."

## Properties

- `Ptr`: Current pointer being read from.

## Constructors

- `LittleEndianReader(byte* ptr)`: Initializes a new instance of the `LittleEndianReader` struct with the given pointer.

## Methods

### ReadShort

!!! info "Reads a signed 16-bit integer from the current pointer in Little Endian format and advances the pointer."

```csharp
public short ReadShort()
```

### ReadUShort

!!! info "Reads an unsigned 16-bit integer from the current pointer in Little Endian format and advances the pointer."

```csharp
public ushort ReadUShort()
```

### ReadUInt

!!! info "Reads an unsigned 32-bit integer from the current pointer in Little Endian format and advances the pointer."

```csharp
public uint ReadUInt()
```

### ReadInt

!!! info "Reads a signed 32-bit integer from the current pointer in Little Endian format and advances the pointer."

```csharp
public int ReadInt()
```

### ReadShortAtOffset

!!! info "Reads a signed 16-bit integer from the specified offset in Little Endian format without advancing the pointer."

```csharp
public short ReadShortAtOffset(int offset)
```

### ReadIntAtOffset

!!! info "Reads a signed 32-bit integer from the specified offset in Little Endian format without advancing the pointer."

```csharp
public int ReadIntAtOffset(int offset)
```

### ReadLongAtOffset

!!! info "Reads a signed 64-bit integer from the specified offset in Little Endian format without advancing the pointer."

```csharp
public long ReadLongAtOffset(int offset)
```

### ReadUlongAtOffset

!!! info "Reads an unsigned 64-bit integer from the specified offset in Little Endian format without advancing the pointer."

```csharp
public ulong ReadUlongAtOffset(int offset)
```

### Seek

!!! info "Advances the pointer by a specified number of bytes."

```csharp
public void Seek(int offset)
```

## About the Offset Methods

The `LittleEndianReader` struct provides several methods that read from a specific offset without advancing the pointer. 
These methods include `ReadShortAtOffset`, `ReadIntAtOffset`, `ReadLongAtOffset`, and `ReadUlongAtOffset`.

While these methods do not significantly reduce the instruction count, they offer some minor performance advantages,
you can read more about this in [LittleEndianWriter's Section](./LittleEndianWriter.md#about-the-offset-methods)

## Examples

The following examples demonstrate how to use the `LittleEndianReader` struct for various tasks:

### Reading Signed 16-bit Integer

```csharp
byte[] data = { 0x01, 0x00 };  // Represents the number 1 in little endian format.
fixed (byte* ptr = data)
{
    var reader = new LittleEndianReader(ptr);
    short value = reader.ReadShort();
    Console.WriteLine(value); // Outputs: 1
}
```

### Reading Unsigned 32-bit Integer at Specific Offset

```csharp
byte[] data = { 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00 };  // Represents the numbers 1 and 2 in little endian format.
fixed (byte* ptr = data)
{
    var reader = new LittleEndianReader(ptr);
    uint value = reader.ReadUIntAtOffset(4);
    Console.WriteLine(value);  // Outputs: 2
}
```

### Reading Signed 64-bit Integer at Specific Offset

```csharp
byte[] data = { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };  // Represents the numbers 1 and 2 in little endian format.
fixed (byte* ptr = data)
{
    var reader = new LittleEndianReader(ptr);
    long value = reader.ReadLongAtOffset(8);
    Console.WriteLine(value);  // Outputs: 2
}
```

### Advancing the Pointer

```csharp
byte[] data = { 0x01, 0x00, 0x02, 0x00 };  // Represents the numbers 1 and 2 in little endian format.
fixed (byte* ptr = data)
{
    var reader = new LittleEndianReader(ptr);
    reader.Seek(2);  // Skip the first number.
    short value = reader.ReadShort();
    Console.WriteLine(value);  // Outputs: 2
}
```