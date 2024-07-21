# File Header

8 bytes:

- `u8[4]` Magic (`"NXUS"`)
- `u7` [Version/Variant](#versionvariant)
- `u5` [Chunk Size](#chunk-size)
- `u16` [Header Page Count](#header-page-count)
- `u4` [Feature Flags](#feature-flags)

## Version/Variant

!!! info "This field is incremented every time an incompatible change in the format is made."

    A change is considered incompatible if an older version of the library would be unable to
    read (extract) files from the new format.

!!! example "An Example"

    The addition of [HasUserData](./User-Data.md) flag does not constitute a breaking
    change as archives with user data can still be read by older versions of the library.

Size: `7 bits` (0-127)

The numbers correspond to the following file format versions:

0: `1.0.0`

!!! tip "Libraries reading the `Nx` format should check this field to ensure compatibility."

    If the version field is higher than the library's version, it should return
    an error indicating that the file is incompatible and the library should
    be updated.

## Chunk Size

!!! note "Large files are split into several blocks during packing."

    [We refer to these blocks as 'chunks'](./Overview.md#terminology).

    This is done to improve compression & decompression speeds at minimal compression ratio loss.

Stored so the decompressor knows how many chunks a file is split into; and how much memory to allocate.  
Also limits memory use on 4+GB archives.  

Size: `5 bits`, (0-31).  
Parsed as `512 << chunkSize`.  

i.e.

- ChunkSize = 15 is `16777216` (16MiB, i.e. 2^24).
- ChunkSize = 31 is `1099511627776` (1TiB, i.e. 2^40).

!!! warning "Chunk size should always exceed Block size."

!!! warning "Reference implementation does not currently support chunks >1GiB"

### Block Size

The size of an individual [block](./Overview.md#terminology) is not standardized
across an `Nx` archive. Blocks can have variable sizes, provided they are
smaller than the [chunk size](#chunk-size).

Currently, the size of SOLID blocks is limited to (`64MiB - 1`) as it is tied to
[the `DecompressedBlockOffset` field in Table of Contents](./Table-Of-Contents.md).

## Header Page Count

Number of 4K memory pages required to store this header, the Table of Contents
(incl. compressed StringPool) and any user data.  

Size: `16 bits`, (0-65535).  
Max size of 256MiB.  

```csharp
return 4096 * tocPageCount;
```

!!! note ""

    The header are padded to the number of bytes stored here.

    Directly after the number of bytes dictated by the `Header Page Count` field
    are the compressed blocks that contain the files.

!!! tip "Reference Numbers (Size)"

    Based on 150+ game `SteamApps` folder (180k files) on Sewer's PC:

    - FileEntries: 4.3MB ([Version 1](./Table-Of-Contents.md#version))
    - Blocks: 1M
    - StringPool: 660K (3.3MB uncompressed)

    Total: 5.96MB

### How the Header is Used

Nx archives are usually consumed in the following manner:

1. Fetch Header
2. Parse Header
3. Do stuff based on what's in the header

The abstractions in Nx allow for the archives to be sourced from anywhere,
so fetching the header could really, for example be downloading the header
from a web server. (e.g. using HTTP's `Content-Range` header)

To optimize reading the header, it is placed up front, and packed.
[In most (>90%) cases](./Table-Of-Contents.md#performance-considerations),
the header fits in a single 4K block.

This design effectively allows you to fetch metadata, very quickly, and
then decide if you want to fetch the rest of the archive.

!!! example "An example"

    If you were to use Nx to deliver mod updates, you could compare the hashes
    of your existing files and download only the files that changed.

!!! question "[Why is max size 256MB](#header-page-count)?"

    In practice, around 64MB should be sufficient given the [current limits](./Table-Of-Contents.md#version).

    The header however allows for up to 256MB to allow for extra info via [arbitrary user data](./User-Data.md) to be stored in the header.

## Feature Flags

!!! info

    This is a section for flags which enable/disable additional features. (e.g. Storing additional file metadata)

This section is reserved and currently unused.

Size: `4 bits` (flags)

Bits are laid out in order `X000`.

| BitFlag | Name                          |
|---------|-------------------------------|
| X       | [HasUserData](./User-Data.md) |
| 0       | Reserved                      |
| 0       | Reserved                      |
| 0       | Reserved                      |
