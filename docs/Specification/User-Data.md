!!! info

    [This flag](./File-Header.md#feature-flags) indicates that the archive contains
    additional user specified data after the [Table of Contents](./Table-Of-Contents.md)

!!! warning "This section is subject to change."

    It is not yet implemented in the reference implementation and is
    subject to change.

Example use cases:

- Storing a binary baked-in hashtable to quickly find files by name.
- Storing update information for a mod package if Nx is used to power a package manager.
- Storing file metadata (read/write timestamps, file permissions, etc.)

## User Data

!!! note "In order to maximize the read performance, each section of user data is 8 byte (word) aligned."

### Header

!!! info "The User Data starts with the following header."

8 bytes:

- `u2` [Version](#version)
- `u4` NumExtensions
    - Indexed starting with 1. So value of `0` means `1` extension.
- `u28` CompressedPayloadSize
    - Size of compressed data in bytes.
- `u30` DecompressedSize
    - Decompressed size of the payload data.

After the header is the compressed payload, which contains [Extensions](#extensions).

!!! tip "The length of user data is `CompressedPayloadSize` + 8 (this header)"

!!! note "The data is compressed with ZStndard by default."

    When compression is not used, `CompressedPayloadSize` == `DecompressedSize`.

#### Version

!!! info "This marks the version/variant of the user data section."

- `0`: Current Version
- `1`: Unused
- `2`: Unused
- `3`: MAX [Unused]

### Extensions

!!! info "Each extension has the following format."

An `Extension` has the following format:

- `u32` ExtensionId (Unique Identifier)
- `u32` PayloadSize

After the `PayloadSize` immediately follows the payload data.

This specifies the exact size of the data for this extension.

After the payload data, the current offset is 8 byte aligned (i.e. up to 7 bytes of padding)
are added, and the next extension starts.

## Example Extension: Storing Extended File Attributes

!!! info "An extension that allows you to store extended file attributes."

    - `ExtensionId`: `XFA ` (0x58464100)

This structure could be defined as:

- Entries[NumFiles]

Where `NumFiles` is the number of files in [Table of Contents](Table-Of-Contents.md).

An extension like this would be useful if you for example wish to store extended
file attributes. Such as modify dates, access permissions, etc.

### Entry

- `u64` CreationTime;
- `u64` LastAccessTime;
- `u64` LastWriteTime;
- `u64` ChangeTime;

## Example Extension: HashTable of File Paths

!!! info "A HashTable that allows you to quickly verify if a particular file is present."

    - `ExtensionId`: HSHT (0x48534854)

Such a hashtable would be useful if you want to verify whether a given file
is present in an archive with a minimum amount of work.

!!! example "An example with file format described below."

    100000 Files @ 0.75 Load Factor:

    - 133333 Buckets (133333 * 4 = 533332 bytes)
    - ~100000 Entries (100000 * 16 = 1600000 bytes)

    Total: 2133332 bytes (2.03MiB)

### File Structure

Header:

- `u32` NumBuckets
- `u32` NumEntryHeaders

Post Header:

- Bucket[NumBuckets]
- EntryHeader[NumEntryHeaders]

### Bucket

- `u32` Offset [to BucketItems]

Each offset is a 4 byte offset relative to the end of the `buckets` section that
points to an `EntryHeader`.

Buckets are accessed by hashing the file path (for example with XXH3), and
MOD-ing the hash by the number of buckets.

### EntryHeader

- `u32` NumEntries
- Entries[NumEntries]

#### Entry 

- `u64` Hash
- `u32` FileIndex
    - Index of file in [Table of Contents](./Table-Of-Contents.md)
