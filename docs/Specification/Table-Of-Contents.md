# Table of Contents (TOC)

Header (8 bytes):

- `u2`: [Version](#version)
- `u24`: StringPoolSize
- `u18`: BlockCount
- `u20`: [FileCount](#file-count)

Variable Size:

- `FileEntry[FileCount]`
    - `u64`: FileHash (xxHash64)
    - `u32/u64`: DecompressedSize
    - `u26`: DecompressedBlockOffset [[limits max block size](./File-Header.md#block-size)]
    - `u20`: FilePathIndex (in [StringPool](#string-pool)) [[limits max file count](./File-Header.md#versionvariant)]
    - `u18`: FirstBlockIndex
- [Blocks[BlockCount]](#blocks)
    - `u29` CompressedBlockSize
    - `u3` [Compression](#compression)
- [StringPool](#string-pool)
    - `RawCompressedData...`

## Version

!!! info "This describes the format of the [FileEntry](#file-entries) structure"

- `0`:
    - Most common variant covering 99.99% of cases.
    - 20 byte FileEntry w/ `u32` Size
    - Up to 4GB (2^32) per file and 1 million files.

- `1`:
    - Variant for archives with large files >= 4GB size.
    - 24 byte FileEntry w/ `u64` Size
    - 2^64 bytes per file and 1 million files.

- `3`: 
    - RESERVED.

Remaining bits reserved for possible future revisions.  
Limitation of 1 million files is inferred from [FileEntry -> FilePathIndex](./Table-Of-Contents.md).

## File Count

!!! info "Marks the number of file entries in the TOC."

This number is [[limited to 1 million due to FilePathIndex](#version)].

## File Entries

Use known fixed size and are 4 byte aligned to improve parsing speed; size 20-24 bytes per item depending on variant.

### Implicit Property: Chunk Count

!!! tip

    Files exceeding [Chunk Size](./File-Header.md#chunk-size) span multiple blocks.

Number of blocks used to store the file is calculated as: `DecompressedSize` / [Chunk Size](./File-Header.md#chunk-size), 
and +1 if there is any remainder, i.e. 

```csharp
public int GetChunkCount(int chunkSizeBytes)
{
    var count = DecompressedSize / (ulong)chunkSizeBytes;
    if (DecompressedSize % (ulong)chunkSizeBytes != 0)
        count += 1;

    return (int)count;
}
```

All chunk blocks are stored sequentially.  

## Blocks

Each entry contains raw size of the block; and compression used. This avoids us having to have an offset for each block.

### Compression

Size: `3 bits` (0-7)

- `0`: Copy
- `1`: ZStandard
- `2`: LZ4
- `3-7`: Reserved

!!! note "As we do not store the length of the decompressed data, this must be determined from the compressed block."

## String Pool

!!! note "Nx archives should only use '/' as the path delimiter."

Raw buffer of UTF-8 deduplicated strings of file paths. Each string is null terminated.
The strings in this pool are first lexicographically sorted (to group similar paths together); and then compressed using ZStd.
As for decompression, size of this pool is unknown until after decompression is done; file header should specify sufficient buffer size.

For example a valid (decompressed) pool might look like this:  
`data/textures/cat.png\0data/textures/dog.png`

String length is determined by searching null terminators. We will determine lengths of all strings ahead of time by scanning
for (`0x00`) using SIMD. No edge cases; `0x00` is guaranteed null terminator due to nature of UTF-8 encoding.

See UTF-8 encoding table:

|  Code point range  |  Byte 1  |  Byte 2  |  Byte 3  |  Byte 4  | Code points |
|:------------------:|:--------:|:--------:|:--------:|:--------:|:-----------:|
|  U+0000 - U+007F   | 0xxxxxxx |          |          |          |     128     |
|  U+0080 - U+07FF   | 110xxxxx | 10xxxxxx |          |          |    1920     |
|  U+0800 - U+FFFF   | 1110xxxx | 10xxxxxx | 10xxxxxx |          |    61440    |
| U+10000 - U+10FFFF | 11110xxx | 10xxxxxx | 10xxxxxx | 10xxxxxx |   1048576   |

When parsing the archive; we decode the StringPool into an array of strings.

!!! note

    It is possible to make ZSTD dictionaries for individual game directories that would further improve StringPool compression ratios.  

    This might be added in the future but is currently not planned until additional testing and a backwards compatibility 
    plan for decompressors missing the relevant dictionaries is decided.

## Performance Considerations

The header + TOC design aim to fit under 4096 bytes when possible. Based on a small 132 Mod, 7 Game Dataset, it is expected that >=90% of
mods out there will fit. This is to take advantage of read granularity; more specifically:

- **Page File Granularity**

For our use case where we memory map the file. Memory maps are always aligned to the page size, this is 4KiB on Windows and Linux (by default).
Therefore, a 5 KiB file will allocate 8 KiB and thus 3 KiB are wasted.

- **Unbuffered Disk Read**

If you have storage manufactured in the last 10 years, you probably have a physical sector size of 4096 bytes.

```pwsh
fsutil fsinfo ntfsinfo c:
# Bytes Per Physical Sector: 4096
```

a.k.a. ['Advanced Format'](https://learn.microsoft.com/en-us/windows/win32/fileio/file-buffering#alignment-and-file-access-requirements).
This is very convenient (especially since it matches page granularity); as when we open a mapped file (or even just read unbuffered),
we can read the exact amount of bytes to get header.
