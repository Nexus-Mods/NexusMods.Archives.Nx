# HeaderParser Class

!!! info "The `HeaderParser` class is a utility for parsing `.nx` file header."

You can use this to parse just the header (incl. [Table of Contents](../../Specification/Table-Of-Contents.md)) if you don't want to extract the files.

## Methods

### ParseHeader

!!! info "Parses the header from a given data provider."

!!! tip "This is a wrapper around [TryParseHeader](#tryparseheader) that does automatic error handling and extracts the full header."

The difference between this and [TryParseHeader](#tryparseheader) is that in more exotic settings like downloading header
over a network, [TryParseHeader](#tryparseheader) will return immediately on insufficient data, while `ParseHeader` 
might stall until all data is available.

```csharp
static unsafe ParsedHeader ParseHeader(IFileDataProvider provider, bool hasLotsOfFiles = false)
```

#### Parameters

- `provider`: Provides the header data.
- `hasLotsOfFiles`: This is a hint to the parser whether the file to be parsed contains lots of individual files (100+).

#### Returns

A successfully parsed `ParsedHeader` instance.

#### Exceptions

- `NotANexusArchiveException`: Not a Nexus Archive.

### TryParseHeader

!!! info "Tries to read the header from the given data."

```csharp
public static unsafe HeaderParserResult TryParseHeader(byte* data, int dataSize)
```

#### Parameters

- `data`: Pointer to header data.
- `dataSize`: Number of bytes available at `data`.

#### Returns

A result with `HeaderParserResult.Header` not null if parsed, else `HeaderParserResult.Header` is null and you should 
call this method again with a larger `dataSize`. The required number of bytes is specified in `HeaderParserResult.HeaderSize`.

#### Exceptions

- `NotANexusArchiveException`: Not a Nexus Archive.

## HeaderParserResult Struct

!!! info "Stores the result of parsing header."

### Properties

- `Header`: The parsed header to use with extraction logic. If this header is null, insufficient bytes are available and you should get required header size from `HeaderSize` and call the method again once you have enough bytes.
- `HeaderSize`: Required size of the header + toc in bytes.

### Constructors

- `HeaderParserResult()`: Creates a `HeaderParserResult`.

## Examples

### Parsing a Header

```csharp
var provider = new FromArrayProvider() { Data = data };
var parsedHeader = HeaderParser.ParseHeader(provider);
```

### Trying to Parse a Header

```csharp
var provider = new SomeNetworkProvider(someUrl);
using var data = provider.GetFileData(0, (uint)headerSize);
var result = TryParseHeader(data.Data, headerSize);
if (result.Header != null)
    Console.WriteLine("Header parsed successfully!");
else
    Console.WriteLine("Failed to parse header. Required header size: " + result.HeaderSize);
```