# Benchmarks

!!! info "Spoiler: This bottlenecks any NVMe 😀"

!!! info

    All tests were executed in the following environment:  
    - `Library Version`: 0.3.0-preview (17th May 2023)  
    - `zstd version`: 1.5.2 (MSVC)  
    - `lz4 version`: [K4os.Compression.LZ4 1.3.5](https://github.com/MiloszKrajewski/K4os.Compression.LZ4)  
    - `CPU`: AMD Ryzen 9 5900X (12C/24T)  
    - `RAM`: 32GB DDR4-3000 (16-17-17-35)  
    - `OS`: Windows 11 22H2 (Build 22621)  
    - `Storage`: Samsung 980 Pro 1TB (NVMe) [PCI-E 3.0 x4]  

!!! note "Some of the compression benchmarks are very dated, as ZStd 1.5.5 has has huge improvements in handling incompressible data since."

    Nonetheless, hopefully they are useful for reference.

## Common Data Sets

!!! tip "These are the data sets which are used in multiple benchmarks below."

### Textures

!!! tip "[Test Data: Skyrim 202X 8.6 Update](https://www.nexusmods.com/Core/Libs/Common/Widgets/DownloadPopUp?id=249831&game_id=1704)"

!!! info "This dataset primarily consists of mostly DDS BC7 textures, with max dimension of `*1024` to `*8192` with a total size of 2.11GB"

Texture overhauls in games make a majority of mods which large file sizes out there.  
Therefore, having a good compression ratio on this data set is important.  

### Log Files

!!! info "This dataset consists of 189 [Reloaded-II Logs](https://reloaded-project.github.io/Reloaded-II/) from end users, across various games, with a total size of 12.4MiB"

### Lightly Compressed Files

!!! info "This dataset consists of every `.one` archive from the 2004 Release of Sonic Heroes, with non-English files removed (168 MiB total)."

Sometimes mods have to ship using games' native compression and/or formats; in which case, they are not very highly compressible,
as the data is already compressed.  

Many older games and some remasters of older games, use custom compression. This compression is usually some variant of basic LZ77.
In this case, we test on data compressed using [SEGA's PRS Compression Scheme](https://github.com/Sewer56/dlang-prs), based on
LZ77 with Run Length Encoding [RLE]. This was a common compression scheme in many SEGA games over ~15 or so years.  

This data set was thrown in as a bonus, to see what happens!

## Block Size (Logs)

!!! info "Investigates the effect of block size on large files with repeating patterns."

!!! tip "[Test Data: Log Files](#log-files)"

!!! note "This test was not in-memory, thus throughput is limited by NVMe bottlenecks. Throughput is provided for reference only."

### ZStandard Only

Applied level applies to both `chunked` and `solid` compression levels.

| Level | Block Size | Size      | Ratio (Size) | Throughput     | Ratio (Throughput) |
|-------|------------|-----------|--------------|----------------|--------------------|
| -1    | 32767      | 1,150,976 | 1.778        | 239.74MiB/s    | 1.944              |
| -1    | 65535      | 1,077,248 | 1.665        | 226.90MiB/s    | 1.839              |
| -1    | 131071     | 970,752   | 1.500        | 215.36MiB/s    | 1.746              |
| -1    | 262143     | 872,448   | 1.348        | 186.86MiB/s    | 1.515              |
| -1    | 524287     | 770,048   | 1.190        | 169.42MiB/s    | 1.373              |
| -1    | 1048575    | 708,608   | 1.095        | 153.09MiB/s    | 1.241              |
| -1    | 2097151    | 667,648   | 1.032        | 153.09MiB/s    | 1.241              |
| -1    | 4194303    | 659,456   | 1.019        | 136.63MiB/s    | 1.107              |
| -1    | 8388607    | 647,168   | 1.000        | 123.36MiB/s    | 1.000              |

| Level | Block Size | Size    | Ratio (Size) | Throughput  | Ratio (Throughput) |
|-------|------------|---------|--------------|-------------|--------------------|
| 9     | 32767      | 909,312 | 2.220        | 204.94MiB/s | 2.032              |
| 9     | 65535      | 819,200 | 2.001        | 192.52MiB/s | 1.908              |
| 9     | 131071     | 733,184 | 1.790        | 186.86MiB/s | 1.852              |
| 9     | 262143     | 630,784 | 1.541        | 181.52MiB/s | 1.800              |
| 9     | 524287     | 548,864 | 1.340        | 167.19MiB/s | 1.658              |
| 9     | 1048575    | 495,616 | 1.210        | 156.87MiB/s | 1.556              |
| 9     | 2097151    | 442,368 | 1.080        | 149.49MiB/s | 1.482              |
| 9     | 4194303    | 413,696 | 1.010        | 127.06MiB/s | 1.259              |
| 9     | 8388607    | 409,600 | 1.000        | 100.84MiB/s | 1.000              |

| Level | Block Size | Size    | Ratio (Size) | Throughput | Ratio (Throughput) |
|-------|------------|---------|--------------|------------|--------------------|
| 16    | 32767      | 884,736 | 2.180        | 71.38MiB/s | 2.754              |
| 16    | 65535      | 790,528 | 1.949        | 69.43MiB/s | 2.677              |
| 16    | 131071     | 712,704 | 1.756        | 64.50MiB/s | 2.486              |
| 16    | 262143     | 598,016 | 1.474        | 52.72MiB/s | 2.034              |
| 16    | 524287     | 548,864 | 1.353        | 90.12MiB/s | 3.476              |
| 16    | 1048575    | 491,520 | 1.212        | 76.09MiB/s | 2.934              |
| 16    | 2097151    | 442,368 | 1.091        | 64.50MiB/s | 2.486              |
| 16    | 4194303    | 413,696 | 1.020        | 44.27MiB/s | 1.708              |
| 16    | 8388607    | 405,504 | 1.000        | 25.93MiB/s | 1.000              |

Level 16 doesn't yield much improvement; lower levels are already good with repeating data.

| Level | Block Size | Size    | Ratio (Size) | Throughput  | Ratio (Throughput) |
|-------|------------|---------|--------------|-------------|--------------------|
| 22    | 131071     | 696,320 | 2.072        | 15.55MiB/s  | 3.945              |
| 22    | 524287     | 512,000 | 1.525        | 17.97MiB/s  | 4.561              |
| 22    | 1048575    | 438,272 | 1.305        | 18.23MiB/s  | 4.626              |
| 22    | 8388607    | 335,872 | 1.000        | 3.94MiB/s   | 1.000              |

Level 22 excels at large blocks due to larger window size, but that's too slow.

### LZ4 Only

| Level | Block Size | Size      | Ratio (Size) | Throughput  | Ratio (Throughput) |
|-------|------------|-----------|--------------|-------------|--------------------|
| 12    | 32767      | 1,159,168 | 1.490        | 158.83MiB/s | 4.326              |
| 12    | 65535      | 1,069,056 | 1.373        | 153.09MiB/s | 4.169              |
| 12    | 131071     | 983,040   | 1.263        | 138.11MiB/s | 3.764              |
| 12    | 262143     | 913,408   | 1.174        | 125.81MiB/s | 3.428              |
| 12    | 524287     | 839,680   | 1.079        | 112.45MiB/s | 3.064              |
| 12    | 1048575    | 806,912   | 1.037        | 116.57MiB/s | 3.176              |
| 12    | 2097151    | 786,432   | 1.011        | 92.75MiB/s  | 2.526              |
| 12    | 4194303    | 786,432   | 1.011        | 68.31MiB/s  | 1.860              |
| 12    | 8388607    | 778,240   | 1.000        | 36.72MiB/s  | 1.000              |

## Block Size (Recompressed Files)

!!! info "Investigates the effect of block size on already lightly compressed data (w/ uncompressed headers)."

!!! tip "[Test Data: Lightly Compressed Files](#lightly-compressed-files)"

!!! note "This test was not in-memory, thus throughput may be subject to NVMe bottlenecks."

!!! note "ZStd 1.5.4 and above have large improvements for uncompressible data handling performance; but only 1.5.2 was available at time of testing."

### ZStandard Only

| Level | Block Size | Size        | Ratio (Size) | Throughput     | Ratio (Throughput) |
|-------|------------|-------------|--------------|----------------|--------------------|
| 9     | 32767      | 139,653,120 | 1.355        | 163.88MiB/s    | 1.068              |
| 9     | 65535      | 139,575,296 | 1.354        | 163.77MiB/s    | 1.068              |
| 9     | 262143     | 136,527,872 | 1.321        | 162.61MiB/s    | 1.060              |
| 9     | 524287     | 135,581,696 | 1.314        | 160.95MiB/s    | 1.049              |
| 9     | 1048575    | 129,404,928 | 1.253        | 153.41MiB/s    | 1.000              |
| 9     | 2097151    | 122,429,440 | 1.186        | 174.43MiB/s    | 1.137              |
| 9     | 4194303    | 113,074,176 | 1.092        | 193.88MiB/s    | 1.264              |
| 9     | 8388607    | 105,893,888 | 1.000        | 209.17MiB/s    | 1.363              |

| Level | Block Size | Size        | Ratio (Size) | Throughput  | Ratio (Throughput) |
|-------|------------|-------------|--------------|-------------|--------------------|
| 16    | 32767      | 137,244,672 | 1.323        | 103.38MiB/s | 1.064              |
| 16    | 262143     | 134,094,848 | 1.291        | 102.71MiB/s | 1.058              |
| 16    | 1048575    | 127,381,504 | 1.227        | 99.98MiB/s  | 1.029              |
| 16    | 4194303    | 111,185,920 | 1.071        | 102.58MiB/s | 1.056              |
| 16    | 8388607    | 103,788,544 | 1.000        | 97.13MiB/s  | 1.000              |

| Level | Block Size | Size        | Throughput   |
|-------|------------|-------------|--------------|
| -1    | 8388607    | 154,382,336 | 1297.30MiB/s |

It seems ZStd can improve on existing LZ77-only compression schemes, in cases where Huffman coding is available.  

This is why only levels `> -1` show improvement.  

### LZ4 Only

| Level | Block Size | Size        | Ratio (Size) | Throughput   | Ratio (Throughput) |
|-------|------------|-------------|--------------|--------------|--------------------|
| 12    | 32767      | 155,537,408 | 1.021        | 319.52MiB/s  | 1.146              |
| 12    | 262143     | 153,378,816 | 1.007        | 340.32MiB/s  | 1.221              |
| 12    | 1048575    | 152,670,208 | 1.003        | 371.06MiB/s  | 1.331              |
| 12    | 4194303    | 152,317,952 | 1.000        | 343.02MiB/s  | 1.231              |
| 12    | 8388607    | 152,264,704 | 1.000        | 278.74MiB/s  | 1.000              |

## Chunk Size (Textures)

!!! info "Investigates the effect of chunk size on large, well compressible files."

!!! tip "[Test Data: Texture](#textures)"

### ZStandard Only

Note: Due to intricacies of ZStd, Chunk Size 1MiB is left out from results as it produces the same output as 4MiB.

| Level | Chunk Size | Size          | Ratio (Size) | Throughput   | Ratio (Throughput) |
|-------|------------|---------------|--------------|--------------|--------------------|
| -1    | 4194304    | 2,211,012,608 | 1.0003       | 1554.50MiB/s | 1.000              |
| -1    | 8388608    | 2,210,525,184 | 1.0001       | 1738.17MiB/s | 1.118              |
| -1    | 16777216   | 2,210,295,808 | 1.000        | 1900.24MiB/s | 1.222              |

| Level | Chunk Size | Size           | Ratio (Size) | Throughput   | Ratio (Throughput) |
|-------|------------|----------------|--------------|--------------|--------------------|
| 9     | 4194304    | 1,802,686,464  | 1.009        | 142.77MiB/s  | 1.061              |
| 9     | 8388608    | 1,791,832,064  | 1.003        | 134.58MiB/s  | 1.000              |
| 9     | 16777216   | 1,786,757,120  | 1.000        | 143.72MiB/s  | 1.068              |

| Level | Chunk Size | Size           | Ratio (Size) | Throughput  | Ratio (Throughput) |
|-------|------------|----------------|--------------|-------------|--------------------|
| 16    | 4194304    | 1,784,340,480  | 1.014        | 73.38MiB/s  | 1.233              |
| 16    | 8388608    | 1,767,153,664  | 1.005        | 66.71MiB/s  | 1.121              |
| 16    | 16777216   | 1,759,150,080  | 1.000        | 59.52MiB/s  | 1.000              |

| Level | Chunk Size | Size           | Ratio (Size) | Throughput  | Ratio (Throughput) |
|-------|------------|----------------|--------------|-------------|--------------------|
| 22    | 4194304    | 1,769,832,448  | 1.023        | 50.76MiB/s  | 1.565              |
| 22    | 8388608    | 1,747,853,312  | 1.010        | 42.33MiB/s  | 1.304              |
| 22    | 16777216   | 1,730,514,944  | 1.000        | 32.45MiB/s  | 1.000              |

### LZ4 Only

!!! tip "LZ4 will have some niche applications; this is one of them. Fast texture loading."

| Level | Chunk Size | Size           | Ratio (Size) | Throughput   | Ratio (Throughput) |
|-------|------------|----------------|--------------|--------------|--------------------|
| 12    | 4194304    | 2,013,491,200  | 1.001        | 364.04MiB/s  | 1.103              |
| 12    | 8388608    | 2,012,303,360  | 1.000        | 357.71MiB/s  | 1.084              |
| 12    | 16777216   | 2,011,750,400  | 1.000        | 330.12MiB/s  | 1.000              |

LZ4 does a huge sacrifice of file size for decompression speeds.  
Depending on use case; this might be okay, but for longer term archiving ZStd Level 9+ is preferred.  

## Thread Scaling: Packing

!!! tip "[Test Data: Texture](#textures)"

!!! tip "Files were packed with 16MB Chunk Size, Chunk Size did not seem to have an effect on compression speed."

### ZStandard Only

!!! info "Packing Speed with ZStd Only, Compression Level"

!!! note "Native ZStd library used, speeds are within margin of error on all runtimes."

| Level | Thread Count | Throughput     | Ratio (Throughput) |
|-------|--------------|----------------|--------------------|
| -1    | 1            | 567.11MiB/s    | 1.000              |
| -1    | 2            | 904.47MiB/s    | 1.595              |
| -1    | 3            | 1190.45MiB/s   | 2.099              |
| -1    | 4            | 1308.24MiB/s   | 2.306              |

I (Sewer) cannot test more than 4 threads on my system due to I/O bottlenecks, but I expect the scaling to continue linearly.

| Level | Thread Count | Throughput     | Ratio (Throughput) |
|-------|--------------|----------------|--------------------|
| 9     | 1            | 60.22MiB/s     | 1.00               |
| 9     | 2            | 92.33MiB/s     | 1.53               |
| 9     | 3            | 117.53MiB/s    | 1.95               |
| 9     | 4            | 134.76MiB/s    | 2.24               |
| 9     | 8            | 179.55MiB/s    | 2.98               |
| 9     | 12           | 172.57MiB/s    | 2.86               |
| 9     | 24 (SMT)     | 142.57MiB/s    | 2.37               |

| Level | Thread Count | Throughput    | Ratio (Throughput) |
|-------|--------------|---------------|--------------------|
| 16    | 1            | 8.79MiB/s     | 1.00               |
| 16    | 2            | 13.49MiB/s    | 1.53               |
| 16    | 3            | 18.70MiB/s    | 2.13               |
| 16    | 4            | 22.45MiB/s    | 2.56               |
| 16    | 8            | 37.25MiB/s    | 4.24               |
| 16    | 12           | 46.25MiB/s    | 5.26               |
| 16    | 24 (SMT)     | 59.71MiB/s    | 6.79               |

### LZ4 Only

!!! info "Packing Speed with LZ4 Only, Compression Level 12"

| Thread Count | Throughput   | Ratio (Throughput) |
|--------------|--------------|--------------------|
| 1            | 32.00 MiB/s  | 1.00               |
| 2            | 52.94 MiB/s  | 1.65               |
| 3            | 82.22 MiB/s  | 2.57               |
| 4            | 102.72 MiB/s | 3.21               |
| 8            | 192.76 MiB/s | 6.02               |
| 12           | 256.30 MiB/s | 8.01               |
| 24 (SMT)     | 346.98 MiB/s | 10.84              |

Scaling for LZ4 is mostly linear with real core count.

## Thread Scaling: Extraction

!!! note "All benchmarks under this section are in-memory; due to exceeding speeds achievable by consumer grade NVMe."

!!! tip "Due to the layout of archive format; the nature of test data (i.e. many small files vs big files) has no effect on performance. (Outside of the standard FileSystem/Storage inefficiencies if writing many small files to disk)."

!!! tip "[Test Data: Texture](#textures)"

Some reference speeds:

| Compression Method                      | Speed        |
|-----------------------------------------|--------------|
| memcpy                                  | ~16.62 GiB/s |
| lz4 1.9.2 (native), 1 thread, [lzbench] | ~4.204 GiB/s |

### ZStandard Only

!!! info "Decompression Speed when Extracting with ZStandard Only"

!!! tip "Files were packed with 16MB Chunk Size, and ZStd Level 16"

!!! note "Native ZStd library used, speeds are within margin of error on all runtimes."

.NET 7 / 8 Preview 3

| Thread Count | Speed          |
|--------------|----------------|
| 1            | ~1.01 GiB/s    |
| 2            | ~1.99 GiB/s    |
| 3            | ~2.87 GiB/s    |
| 4            | ~3.70 GiB/s    |
| 6            | ~5.15 GiB/s    |
| 8            | ~6.35 GiB/s    |
| 10           | ~7.38 GiB/s    |
| 12           | ~7.81 GiB/s    |
| 24           | ~7.19 GiB/s ‼️ |

Observed dropoff with hyperthreading, presumably due to cache inefficiency.  
Ideally we could detect real core count; but this is hard; it is deliberately abstracted.

### LZ4 Only

!!! info "Decompression Speed when Extracting with LZ4 Only"

!!! tip "Files were packed with 16MB Chunk Size, and LZ4 Level 12"

.NET 7

| Thread Count | Speed           |
|--------------|-----------------|
| 1            | ~2.83 GiB/s     |
| 2            | ~5.56 GiB/s     |
| 3            | ~8.09 GiB/s     |
| 4            | ~10.00 GiB/s    |
| 6+           | ~11.63 GiB/s    |

.NET 8 Preview 3

| Thread Count | Speed        |
|--------------|--------------|
| 1            | ~3.82 GiB/s  |
| 2            | ~7.36 GiB/s  |
| 3            | ~10.41 GiB/s |
| 4+           | ~11.72 GiB/s |
| 8+           | ~12.10 GiB/s |

.NET 8 Preview 3 NativeAOT

| Thread Count | Speed        |
|--------------|--------------|
| 1            | ~3.26 GiB/s  |
| 2            | ~6.33 GiB/s  |
| 3            | ~9.20 GiB/s  |
| 4            | ~11.09 GiB/s |
| 6+           | ~11.93 GiB/s |

## Presets

The following presets have been created...

### Fast/Random Access Preset

!!! info "This preset is designed for optimising random access, and intended in use when low latency previews are needed such as in the [Nexus App](https://github.com/Nexus-Mods/NexusMods.App)."

- `Solid Algorithm: ZStandard`  
- `Chunked Algorithm: ZStandard`  
- `Solid Compression Level: -1`  
- `Chunked Compression Level: 9`  

### Archival/Upload Preset

!!! info "This preset is designed for all other use cases. Providing a fair balance for all other use cases."

- `Solid Algorithm: ZStandard`  
- `Chunked Algorithm: ZStandard`  
- `Solid Compression Level: 16`  
- `Chunked Compression Level: 9`  

## Comparison to Common Archiving Solutions

!!! info "Tests here were ran on .NET 8 Preview 3 NativeAOT, there aren't currently any significant differences here between runtimes."

!!! note "All tests were ran under optimal thread count/best case scenario."

!!! note "NX is entirely I/O bottlenecked here (on PCI-E 3.0 drive). Therefore in-memory benchmarks are also provided."

### Compression Scripts

Zip (Maximum):
```batch
"7z.exe" a -tzip -mtp=0 -mm=Deflate -mmt=on -mx7 -mfb=64 -mpass=3 -bb0 -bse0 -bsp2 -mtc=on -mta=on "output" "input" 
```

Zip (Maximum, Optimized):  
```batch
"7z.exe" a -tzip -mtp=0 -mm=Deflate -mmt=on -mx7 -mfb=64 -mpass=1 -bb0 -bse0 -bsp2 -mtc=on -mta=on "output" "input" 
```

7z (Normal):
```batch
7z.exe" a -t7z -m0=LZMA2 -mmt=on -mx5 -md=16m -mfb=32 -ms=4g -mqs=on -sccUTF-8 -bb0 -bse0 -bsp2 -mtc=on -mta=on "output" "input" 
```

7z (Ultra):
```batch
7z.exe" a -t7z -m0=LZMA2 -mmt=on -mx9 -md=64m -mfb=64 -ms=16g -mqs=on -sccUTF-8 -bb0 -bse0 -bsp2 -mtc=on -mta=on "output" "input" 
```

Nx (Archival Preset):
```batch
NexusMods.Archives.Nx.Cli.exe pack --source "input" --target "output" --solid-algorithm ZStandard --chunked-algorithm ZStandard --solidlevel 16 --chunkedlevel 9
```

Nx (Random Access Preset):
```batch
NexusMods.Archives.Nx.Cli.exe pack --source "input" --target "output" --solid-algorithm ZStandard --chunked-algorithm ZStandard --solidlevel -1 --chunkedlevel 9
```

!!! note "Zip (Optimized) cannot be set via GUI, only via CMD parameter."

### Unpacking Scripts

7z/Zip:  
```batch
7z.exe" x -aos "-ooutput" -bb0 -bse0 -bsp2 -pdefault -sccUTF-8 -snz "input"
```

Nx:  

```batch
./NexusMods.Archives.Nx.Cli.exe extract --source input --target "output"
```

### Textures

!!! tip "[Test Data: Texture](#textures)"

Packing:  

| Method                  | Time Taken | Ratio (Time) | Size          | Ratio (Size) |
|-------------------------|------------|--------------|---------------|--------------|
| Zip (Maximum)           | 17.385s    | 2.34         | 1,957,310,894 | 1.24         |
| Zip (Optimized)         | 7.443s     | 1.00         | 1,961,872,315 | 1.25         |
| 7z (Normal)             | 73.572s    | 9.88         | 1,616,082,220 | 1.03         |
| 7z (Ultra)              | 120.746s   | 16.23        | 1,570,741,070 | 1.00         |
| Nx (Random Access, 12T) | 12.909s    | 1.73         | 1,787,568,128 | 1.14         |
| Nx (Archival, 12T)      | 12.955s    | 1.74         | 1,786,634,240 | 1.14         |

Unpacking: 

| Method               | Time Taken | Ratio (Time) |
|----------------------|------------|--------------|
| Zip                  | 13.857s    | 50.76        |
| 7z (Ultra)           | 6.238s     | 22.86        |
| 7z (Normal)          | 3.705s     | 13.57        |
| Nx (12T)             | 1.172s     | 4.29         |
| Nx [In-Memory] (12T) | 0.273s     | 1.00         |

### Logs

Packing:

| Method                  | Time Taken | Ratio (Time) | Size    | Ratio (Size) |
|-------------------------|------------|--------------|---------|--------------|
| Zip (Maximum)           | 0.149s     | 2.07         | 758,928 | 2.18         |
| Zip (Optimized)         | 0.115s     | 1.60         | 768,374 | 2.21         |
| 7z (Normal)             | 0.297s     | 4.13         | 378,780 | 1.09         |
| 7z (Ultra)              | 0.545s     | 7.57         | 347,574 | 1.00         |
| Nx (Archival, 12T)      | 0.194s     | 2.69         | 491,520 | 1.41         |
| Nx (Random Access, 12T) | 0.072s     | 1.00         | 708,608 | 2.04         |


Unpacking:

| Method                              | Time Taken | Ratio (Time) |
|-------------------------------------|------------|--------------|
| Zip                                 | 0.214s     | 167.19       |
| 7z (Ultra)                          | 0.225s     | 175.78       |
| 7z (Normal)                         | 0.267s     | 208.59       |
| Nx (Random Access)                  | 0.127s     | 99.22        |
| Nx (Archival)                       | 0.127s     | 99.22        |
| Nx (Random Access) [In-Memory] (1T) | 0.00304s   | 2.38         |
| Nx (Archival) [In-Memory] (1T)      | 0.00300s   | 2.34         |
| Nx (Random Access) [In-Memory] (4T) | 0.0016s    | 1.25         |
| Nx (Archival) [In-Memory] (4T)      | 0.00128s   | 1.00         |

!!! note "Extraction is I/O bottlenecked; Windows is slow to create small files."

!!! note "Random Access mode is not faster here due to nature of data set. For larger compressed blocks however, it achieves ~3.5x speed."