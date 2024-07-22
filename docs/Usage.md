## High Level API

!!! info

    The recommended way to use the library is through the high level *Fluent* API.

### Packing

!!! info "Use the `NxPackerBuilder` API to fluently create a new archive."

```csharp
var builder = new NxPackerBuilder();
builder.AddFolder(source);
builder.WithOutput(new FileStream(target, FileMode.Create, FileAccess.ReadWrite));

// Set some settings
if (blocksize.HasValue) builder.WithBlockSize(blocksize.Value);
if (chunksize.HasValue) builder.WithBlockSize(chunksize.Value);
if (solidLevel.HasValue) builder.WithSolidCompressionLevel(solidLevel.Value);
if (chunkedLevel.HasValue) builder.WithChunkedLevel(chunkedLevel.Value);
if (solidAlgorithm.HasValue) builder.WithSolidBlockAlgorithm(solidAlgorithm.Value);
if (chunkedAlgorithm.HasValue) builder.WithChunkedFileAlgorithm(chunkedAlgorithm.Value);
if (threads.HasValue) builder.WithMaxNumThreads(threads.Value);
builder.WithChunkedDeduplication(deduplicateChunked); // deduplicate chunks during packing
builder.WithSolidDeduplication(deduplicateSolid); // deduplicate files in SOLID blocks during packing

// Make the Archive
builder.Build(); // Blocking
```

### Unpacking

!!! info "Use the `NxUnpackerBuilder` API to fluently extract an archive."

```csharp
// Make the builder.
var stream = new FileStream(source, FileMode.Open, FileAccess.Read);
var builder = new NxUnpackerBuilder(new FromStreamProvider(stream));

// Output all files to disk.
builder.AddFilesWithDiskOutput(builder.GetFileEntriesRaw(), outputDirectory);

if (threads.HasValue)
    builder.WithMaxNumThreads(threads.Value);
    
// Extract the archive.
builder.Extract(); // Blocking
```

### Repacking

!!! info "Use the `NxRepackerBuilder` and its derivatives for 'repacking' archives."

    In this case 'repacking' means taking an existing archive and adding/removing files
    to/from it in an efficient manner.

The `NxRepackerBuilder` is an extension of [NxPackerBuilder](#packing), so you
can use it as a drop-in replacement.

It allows you to add files which are already present in an existing Nx archive
in an extremely efficient manner.

!!! tip "Under the hood this enables for"

Use cases of this include:

- Combining multiple Nx archives into one.
- Quick deletion of files from an existing Nx archive.
- Quickly packing an updated version of a mod (file) based off of an older version.

Example:

```csharp
// Use any IFileDataProvider to provide the existing archive.
// Ideally memory mapped provider, i.e. FromFilePathProvider
// if file is on disk.
var provider = new FromFilePathProvider() {
    FilePath = "existing.nx"  
};
var header = HeaderParser.ParseHeader(provider);

var repackerBuilder = new NxRepackerBuilder();

// Add files from existing archive
repackerBuilder.AddFilesFromNxArchive(nxSource, header, header.Entries.AsSpan());

// Configure output
repackerBuilder.WithOutput(new FileStream("repacked.nx", FileMode.Create));

// Build the repacked archive
using var outputStream = repackerBuilder.Build();
```

Key methods:

- `AddFileFromNxArchive`: Adds a single file from an archive.
- `AddFilesFromNxArchive`: Adds multiple files from an archive.

!!! warning "Repacking and Deduplication logic are purely hash based."

    There is a tiny, insignificant chance of unintended consequences if a hash
    collision occurs.

!!! warning "Only repacking of Nx archives with equal chunk sizes is supported."

    If two archives have non-matching chunk sizes, the repacker will throw an error.

#### Merging Archives

!!! tip "Use `NxDeduplicatingRepackerBuilder` for merging archives."

This is a derivative of `NxRepackerBuilder` which automatically deduplicates files
by hash as they are added.

## Mid Level API

!!! info

    Lower level API is for those who want to get more control over the packing process.

### Packing

!!! info "For packing archives, use the `NxPacker.Pack` API."

Example:

```csharp
using var output = File.Create("archive.nx");
var settings = new PackerSettings { Output = output };
var files = FileFinder.GetFiles("some/folder/path");
NxPacker.Pack(files, settings);
```

In this example, we get the files to pack from a directory using the `FileFinder` API, specify the settings using `PackerSettings` 
and then call `NxPacker.Pack` to pack the files.

The files parameter is of type `PackerFile[]`, which has the following interface.

```csharp
public class PackerFile : IHasRelativePath, IHasFileSize, IHasSolidType, IHasCompressionPreference, ICanProvideFileData
{
    // IFileDataProvider(s) are provided in NexusMods.Archives.Nx.FileProviders namespace !!
    // They use names called `FromXXXProvider` where XXX is the source.
    public required IFileDataProvider FileDataProvider { get; init; }
    public string RelativePath { get; init; } = string.Empty;
    public long FileSize { get; init; }
    
    // Only honoured if SolidType == NoSolid
    public CompressionPreference CompressionPreference { get; set; } = CompressionPreference.NoPreference;
    public SolidPreference SolidType { get; set; } = SolidPreference.Default;
}
```

This allows you more granular control of where the data to be compressed is sourced from, for example you can create a file
to be packed from an array in memory by doing the following:  

```csharp
return new PackerFile()
{
    FileSize = fileSize,
    RelativePath = $"SomeCoolFile",
    FileDataProvider = new FromArrayProvider { Data = data }
};
```

Or you can request a file to not be placed in any SOLID block by doing the following:  

```csharp
packerFile.CompressionPreference = CompressionPreference.ZStandard;
packerFile.SolidType = SolidPreference.NoSolid;
```

### Unpacking

!!! warning "This API is not thread safe. If you need to unpack multiple archives in parallel, create a new instance of `NxUnpacker` for each concurrent operation."

```csharp
var provider = new FromStreamProvider(fileStream);
var unpacker = new NxUnpacker(provider);
// var onDisk = unpacker.ExtractFilesToDisk(unpacker.GetFileEntriesRaw(), temporaryFilePath.FolderPath, new UnpackerSettings());
// var inMemory = unpacker.ExtractFilesInMemory(unpacker.GetFileEntriesRaw(), new UnpackerSettings());
```

To unpack, use the `NxUnpacker` API. The `NxUnpacker` constructor takes a `IFileDataProvider` as a parameter; i.e. the same interface
as used by files to be packed. 

#### Fetching Files
After using the constructor you can call `unpacker.GetFileEntriesRaw()` to get a list of file entries from within the archive and use
`GetFilePath(FileEntry entry);` to get the names of the returned entries, i.e.

```csharp
Span<FileEntry> entries = unpacker.GetFileEntriesRaw();
foreach (var entry in entries) 
{
    var name = unpacker.GetFilePath(entry);
    
    // Do something with entry.
}
```

#### Extracting to Arbitrary Outputs

!!! info "If you are extracting the files to disk or memory, you can use `unpacker.ExtractFilesToDisk`/`unpacker.ExtractFilesInMemory` directly instead."

!!! info "This example shows how to manually set up extracting to an array."

```csharp
Span<FileEntry> files = unpacker.GetFileEntriesRaw();
// outputs must be covariant with IOutputDataProvider[]
var outputs = new OutputArrayProvider[files.Length];
for (var x = 0; x < files.Length; x++)
{
    var entry = files[x];
    var relPath = unpacker.GetFilePath(entry);
    outputs.DangerousGetReferenceAt(x) = new OutputArrayProvider(relPath, entry);
}
    
// Extract the files.
unpacker.ExtractFiles(outputs, new UnpackerSettings());

// Results are available in `outputs` array.
```

To unpack to an arbitrary source, prepare an array of `IOutputDataProvider` and call `unpacker.ExtractFiles`.  

You can find existing implementations of `IOutputDataProvider` in the `NexusMods.Archives.Nx.FileProviders` namespace, 
they follow the naming convention `OutputXXXProvider`.

!!! warning

    Implementations of `IOutputDataProvider` are disposed by the unpacker after use. Do not manually dispose or use `using` 
    statement; they will be disposed as needed.

## Low Level API

!!! info "Low level APIs, such as those for creating blocks for packing and chunking files are currently not exposed."
