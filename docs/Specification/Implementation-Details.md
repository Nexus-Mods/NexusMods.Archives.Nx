# Implementation Details

!!! info

    This page contains guidance for people implementing their own libraries for working with `.nx` files.  
    It is not a step by step guide.  

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
- Flush to disk every file where all data has been written. (Check once all work per thread done.)  

Most compression algorithms accept `Span<byte>`; and in cases where we might need to do native interop; we of course have pointers.

## Parallel Compression

!!! info

    Compression process involves grouping the files into blocks, then compressing the blocks in parallel.

To do this, we will perform the following steps:  

- Files are sorted by size. (optimize for blocks)  
- Files are then grouped by extension. (optimize for ratio)  
- These groups are chunked into blocks. Files whose size exceeds `BlockSize / 2` starting with first file that doesn't fit into block are allocated their own block.  
- The groups are then re-combined in order of `total size of group data` ascending.  
- We assign the groups to individual threads using a task scheduler; which is simply a ThreadPool that will pick up tasks in the order they are submitted.  

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
    - Use `zstd -11` or `zstd -16` (depending on CPU, estimate power by thread count).  
    - Can't have the user wait too long...  
    - We only use highest levels e.g. `zstd -22` when 'publishing' (uploading) to web.  

- Files which we know are not compressible are assigned `copy` as compression strategy in their blocks.  
    - We will provide a mechanism to feed this info from outside the library.  

- Extraordinarily large files should be handled with LZ4.  
    - To avoid case of lots of small files, and one huge file.  
    - Files 1GB and beyond. Needs benchmarking.  

- Prefer ZStd for large files.

## Repacking/Appending Files

Speed of this operation depends on SOLID block size, but in most cases should be reasonably fast. This is because
non-SOLID blocks used for big files can be copied verbatim.

Updating the ToC is inexpensive.