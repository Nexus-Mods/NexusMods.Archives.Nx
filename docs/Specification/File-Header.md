# File Header

- `u8[4]` Magic (`"NXUS"`)
- `u4` [BlockSize](#block-size)
- `u4` [Version/Variant](#versionvariant)
- `u8` [TocCompressedPageCount](#tocpagecount)
- `u8` [TocPoolDecompressedPageCount](#tocpagecount)
- `u8` [Feature Flags](#feature-flags)

## Block Size

Stored so the decompressor knows how big each block is.

Size: `4 bits`, but restricted (1-12).  
Parsed as `32768 << blockSize`.

i.e. BlockSize = 12 is `67108864` (64MiB, i.e. 2^26).

!!! note

    The limit on block size is restricted for bit-packing purposes in [Table of Contents File Entries](./Table-Of-Contents.md).

## Version/Variant

Size: `4 bits` (0-15)

- `0`:
    - Most common variant covering 99.99% of cases.
    - 20 byte FileEntry w/ `u32` Size
    - Up to 4GB (2^32) per file and 4 million files.

- `1`:
    - Variant for archives with large files >= 4GB size.
    - 32 byte per entry w/ `u64` Size
    - 2^64 bytes per file and 4 million files.

Remaining bits reserved for possible future revisions.

## TocPageCount

Number of 4K memory pages required to store the compressed and decompressed Table of Contents.

- `TocPageCount` represents number of pages needed to store the entire ToC.
- `TocPoolDecompressedPageCount` represents a buffer size large enough to store decompressed `StringPool` (see below).

These values are encoded as `numPages - 1` (so 0 == 1) and converted to bytes the following way:

```csharp
if (tocPageCount >= 248) 
    return (1024 * 1024) << tocPageCount - 8; // 256MiB max.

return 4096 * tocPageCount; // just under 1MiB max.
```

!!! tip

    As a rough reference, StringPool [main memory consumer] of (150+) games (180k files) on Sewer's PC used up 3.3MB uncompressed, 660K compressed.

## Feature Flags

!!! info

    This is a section for flags which enable/disable additional features. (e.g. Storing additional file metadata)

This section is reserved and currently unused.