# File Header

8 bytes:

- `u8[4]` Magic (`"NXUS"`)
- `u3` [Version/Variant](#versionvariant)
- `u4` [BlockSize](#block-size)
- `u4` [Large File Chunk Size](#large-file-chunk-size)
- `u13` [Header Page Count](#header-page-count)
- `u8` [Feature Flags](#feature-flags)

## Version/Variant

Size: `3 bits` (0-7)

- `0`:
    - Most common variant covering 99.99% of cases.
    - 20 byte FileEntry w/ `u32` Size
    - Up to 4GB (2^32) per file and 1 million files.

- `1`:
    - Variant for archives with large files >= 4GB size.
    - 24 byte FileEntry w/ `u64` Size
    - 2^64 bytes per file and 1 million files.

Remaining bits reserved for possible future revisions.  
Limitation of 1 million files is inferred from [FileEntry -> FilePathIndex](./Table-Of-Contents.md).

## Block Size

Stored so the decompressor knows how big each block is.

Size: `4 bits`.  
Parsed as `(4096 << blockSize) - 1`.  

Limited to BlockSize = 14, `67108863` (i.e. `64MiB - 1` or `2^26 - 1`).  
Due to [Table of Contents File Entries](./Table-Of-Contents.md).  

!!! note "A future version/flag may allow 128MiB SOLID blocks, however for now, we haven't found a need for it."

!!! note "We remove -1 from the value to avoid collisions with [Chunk Size](#large-file-chunk-size)"

## Large File Chunk Size

!!! tip

    Large files are split into several chunks during packing to improve compression speeds at minimal compression ratio loss.  

Stored so the decompressor knows how many chunks a file is split into; and how much memory to allocate.  
Also limits memory use on 4+GB archives.  

Size: `4 bits`, (0-15).  
Parsed as `32768 << chunkSize`.  

i.e. ChunkSize = 15 is `1073741824` (1GiB, i.e. 2^31).  

!!! note

    Please do not confuse 'block' and 'chunk'. Blocks segment the compressed data. Chunks segment a file.

!!! warning

    Chunk size should always exceed Block size. Implementation of archiver can either return error or enforce this automatically.

## Header Page Count

Number of 4K memory pages required to store this header and the Table of Contents (incl. compressed StringPool).  

Size: `13 bits`, (0-8191).  
Max size of 32MiB.  

```csharp
return 4096 * tocPageCount;
```

!!! tip

    As a rough reference, StringPool [main memory consumer] of (150+) games (180k files) on Sewer's PC used up 3.3MB uncompressed, 660K compressed.

!!! note

    The headers are padded to the number of bytes stored here.

## Feature Flags

!!! info

    This is a section for flags which enable/disable additional features. (e.g. Storing additional file metadata)

This section is reserved and currently unused.