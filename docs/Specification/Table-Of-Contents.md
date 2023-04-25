# Table of Contents (TOC)

- `u32`: FileCount [[limited to 1 million due to FilePathIndex](./File-Header.md#versionvariant)]
- `u18`: BlockCount
- `u14`: Reserved/Padding
- `FileEntry[FileCount]`
    - `u64`: FileHash (xxHash64)
    - `u32/u64`: DecompressedSize
    - `u26`: DecompressedBlockOffset [[limits max block size](./File-Header.md#block-size)]
    - `u20`: FilePathIndex (in [StringPool](#string-pool)) [[limits max file count](./File-Header.md#versionvariant)]
    - `u18`: FirstBlockIndex
- Blocks[BlockCount]
    - `u32/u64` CompressedBlockSize
- BlockCompressions
    - `u2` Compression
- [Align(u8)](#blockcompressions)
- StringPool (see below)
    - `RawCompressedData...`

## File Entries

Use known fixed size and are 4 byte aligned to improve parsing speed; size 20-24 bytes per item depending on variant.

### Implicit Property: Is SOLID

!!! tip

    Reserved value `DecompressedBlockOffset == 2^26` means 'file is not SOLID'.  

This is why we [take away 1 from Block Size in header](./File-Header.md#block-size).  
This allows us to insert non-SOLID files (even under block size) into single blocks.  

### Implicit Property: Chunk Count

!!! tip

    Files exceeding [Chunk Size](./File-Header.md#large-file-chunk-size) span multiple blocks.

Number of blocks used to store the file is calculated as `DecompressedSize` / [Chunk Size](./File-Header.md#large-file-chunk-size).

## Blocks

Each entry contains raw size of the block; this avoids us having to have an offset for each block.

## BlockCompressions

!!! note "This section is padded to next byte. a.k.a. `Align(u8)"

Size: `2 bits` (0-3)

- `0`: Copy
- `1`: ZStandard
- `2`: LZ4
- `3`: Reserved

If we ever need to add more formats; we still got reserved space for flags in header; and a flag could extend this to 4 bits (16 values).

## String Pool

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