# LittleEndianWriter

The `LittleEndianWriter` is a utility structure for writing to a pointer in Little Endian format. It offers various methods to write different types of data to the pointer and automatically advances the pointer after each write.

## Properties

- `Ptr`: The current pointer being written to.

## Constructor

- `LittleEndianWriter(byte* ptr)`: Creates a `LittleEndianWriter` that wraps around a pointer for writing data in Little Endian format.

## Methods

### Write

!!! info "Writes a value to and advances the pointer."

```csharp
public void Write(short value)
public void Write(ushort value)
public void Write(uint value)
public void Write(int value)
public void Write(long value)
public void Write(ulong value)
public void Write(Span<byte> data)
```

### WriteAtOffset

!!! info "Writes a value to the specified offset without advancing the pointer."

```csharp
public void WriteAtOffset(short value, int offset)
public void WriteAtOffset(int value, int offset)
public void WriteAtOffset(long value, int offset)
public void WriteAtOffset(ulong value, int offset)
```

### Seek

!!! info "Advances the stream by a specified number of bytes."

```csharp
public void Seek(int offset)
```

## About the Offset Methods

The `LittleEndianWriter` struct provides the method `WriteAtOffset` that can be used to write to an offset of the current
pointer without advancing the pointer itself.

These methods offer some minor performance advantages.

### Improved Pipelining

By reducing the dependency of future instructions on earlier instructions, these offset methods allow for better pipelining.
For example, a future read operation does not need to wait for the `Ptr` value to be updated from a previous operation.

### JIT Optimization

The Just-In-Time (JIT) compiler can recognize when the `offset` parameters are specified as constants and can optimize
the instructions accordingly. This can lead to more efficient code execution.

```csharp
writer.WriteAtOffset(Hash, 0);
writer.WriteAtOffset((int)DecompressedSize, 8);
writer.WriteAtOffset(new OffsetPathIndexTuple(DecompressedBlockOffset, FilePathIndex, FirstBlockIndex).Data, 12);
writer.Seek(NativeFileEntryV0.SizeBytes);
```

Because write on `line 1`, does not depend on modified pointer after `line 0`, execution is faster, as the CPU can 
better pipeline the instructions as there is no dependency on the ptr result of the previous method call.

## Examples

### Writing an Integer to The Pointer

```csharp
byte[] data = new byte[4];
fixed (byte* ptr = data)
{
    var writer = new LittleEndianWriter(ptr);
    writer.Write(42); // Writes the integer 42 to the pointer in Little Endian format, and advances.
}
```

### Writing a Short to The Pointer at Specific Offset

```csharp
byte[] data = new byte[4];
fixed (byte* ptr = data)
{
    var writer = new LittleEndianWriter(ptr);
    writer.WriteAtOffset((short)1234, 2); // Writes the short 1234 to the pointer at offset 2 in Little Endian format.
}
```

### Advancing the Pointer

```csharp
byte[] data = new byte[12];
fixed (byte* ptr = data)
{
    var writer = new LittleEndianWriter(ptr);
    writer.Write(42); // Writes the integer 42 to the pointer in Little Endian format, and advances.
    writer.Seek(4); // Advances the pointer by 4 bytes.
    writer.Write(84); // Writes the integer 84 to the new pointer position in Little Endian format, and advances.
}
```