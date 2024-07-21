# Implementation Details

!!! info

    This page contains guidance for people implementing their own libraries for working with `.nx` files.  
    It is not a step by step guide, just some guidelines.  

!!! note

    For the best source of truth, always check the source code!

## Use Memory Maps

!!! tip

    When possible, work with memory mapped files.

- A system call is orders of magnitude slower than change to a program's local memory.   
- It can avoid data copies, as it can map to kernel's page (file) cache; meaning copies of data isn't unnecessarily made in user space.  

## Parallel Decompression

!!! info

    Files are decompressed in parallel by decompressing multiple blocks in separate threads/tasks.

The decoding logic should assigns each block to a given thread; and then each thread performs the following action(s):  

- Allocate and `mmap` output files in advance.  
- Decompress relevant data into output files.  
- Flush to disk every file when all data has been written.  

Most compression algorithms accept `Span<byte>`; and in cases where we might need to do native interop; we of course have pointers.

## Parallel Compression

!!! info

    Compression process involves grouping the files into blocks, then compressing the blocks in parallel.

To do this, we will perform the following steps:  

- Files are sorted by size. (optimize for blocks)  
- Files are then grouped by extension. (optimize for ratio) [while preserving sorting]  
- These groups are chunked into SOLID blocks (and huge files into Chunk blocks).  
    - They are ordered by size descending to maximize CPU utilization. 
    - Chunked blocks appear first. 
        - These are always bigger than SOLID blocks, but cannot be reordered relative to each other as they are sequential.
    - Sorted SOLID blocks are then added to chunked blocks.
- We assign the groups to individual blocks using a task scheduler; which is simply a ThreadPool that will pick up tasks in the order they are submitted.  

!!! note
  
    Grouping files by extension (equivalent to 7zip `qs` parameter) improves compression ratio, as data between different
    files of same type is likely to be similar (e.g. two text files).  

## Choosing Compression Implementation

!!! tip

    There is an important tradeoff between size and speed to be made, 
    especially for more real-time cases like [Nexus Mods App](https://github.com/Nexus-Mods/NexusMods.App) where
    downloaded mods are recompressed on download. 

    The appropriate approach depends on your input data and use case; below are some general tips.

- For repacking downloaded files from the web; prefer faster compression approaches.  
    - Prefer `lz4 -12` or `zstd -16` (depending on CPU, estimate power by thread count).  
    - Can't have the user wait too long...  
    - We only use highest levels e.g. `zstd -22` when 'publishing' (uploading) to web.  

- Files which we know are not compressible are assigned `copy` as compression strategy in their blocks.  
    - We provide a mechanism to feed this info from outside the library.  

- Prefer ZStd for large files.  

## Repacking & Merging Nx Archives

!!! info "Repacking/Merging Archives should be a fairly inexpensive operation."

Namely, it's possible to do the following:

- Copy compressed blocks directly/chunks between Nx archives.
    - Decompression buffer size is determined from the file entries, thus blocks can be copied verbatim.
    - It's possible to mix [SOLID block sizes](./File-Header.md#block-size).
        - Provided that SOLID blocks are smaller than the [Chunk Size](./File-Header.md#chunk-size) of the new archive.
        - Verify this by checking if [Chunk Size](./File-Header.md#chunk-size) of all input archive matches.
- Efficiently use existing SOLID blocks as inputs.
    - Use files inside compressed blocks from File A as input to File B.
    - With clever usage, (Example: [FromExistingNxBlock][from-existing-nx-block]) you can decompress just-in-time.

The runtime complexity/overheads of repacking are generally very low, so the whole
operation should be nearly as fast as just copying the data verbatim.

### Maximizing Multithreaded Packing Efficiency

!!! info "When [Compressing in Parallel](#parallel-compression), additional consideration is needed."

This requires a short explanation.

Although the blocks are being [compressed entirely in parallel](#parallel-compression),
writing out the block to the final output/stream is a sequential operation.

What this means is that if any of the blocks being processed in parallel takes
considerably longer to compress than the others, the entire pipeline will be
bottlenecked by that single block; reducing the overall efficiency.

Because the operation of compressing a block is much slower than copying it from
an existing file to another (by magnitudes), it is recommended that all copied
blocks are processed first before starting to compress new blocks.

In that vain, consider placing all the blocks which do a raw copy at the end,
since they are the fastest to process.

[from-existing-nx-block]: https://github.com/Nexus-Mods/NexusMods.Archives.Nx/blob/ce09b2099f28293ca30a3c634160f1c539ef297c/NexusMods.Archives.Nx/FileProviders/FromExistingNxBlock.cs
