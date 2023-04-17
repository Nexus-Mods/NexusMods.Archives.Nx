# Implementation Details

!!! info

    This page contains guidance for people implementing their own libraries for working with `.nx` files. 

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

First we must decide how to group files into blocks.

To do this, we will perform the following steps:  

- Files are sorted by size. (optimize for blocks)  
- Files are then grouped by extension. (optimize for ratio)  
- These groups are chunked into blocks. Files whose size exceeds `BlockSize / 2` are allocated their own block.  
- The groups are then re-combined in order of `total size of group data` ascending.  
- Groups are assigned to threads, e.g. For 8 groups, 4 threads you assign thread(s) `1,2,3,4` + `1,2,3,4`. This handles load balancing.  

Note: Grouping files by extension (equivalent to 7zip `qs` parameter) improves compression ratio, as data between different
files of same type is likely to be similar (e.g. two text files).  

## Choosing Compression Implementation

- For recompressing downloaded files from the web; prefer faster compression approaches.  
    - Use `zstd -11` or `zstd -16` (depending on CPU, estimate power by thread count).  
    - Can't have the user wait too long...  
    - We only use highest levels e.g. `zstd -22` when 'publishing' (uploading) to web.  

- Files which we know are not compressible are assigned `copy` as compression strategy in their blocks.  
    - We should have some sort of mechanism to feed this info from outside the library.  

- Extraordinarily large files should be handled with LZ4.  
    - To avoid case of lots of small files, and one huge file.  
    - Files 1GB and beyond. Needs benchmarking.  

Prefer ZStd for large files.

## Repacking/Appending Files

Speed of this operation depends on SOLID block size, but in most cases should be reasonably fast. This is because
non-SOLID blocks used for big files can be copied verbatim.

Updating the ToC is inexpensive.