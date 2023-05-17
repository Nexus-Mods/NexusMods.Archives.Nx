# Benchmarks

!!! tip "Coming Soon (TM)"

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

## Packing Speed

!!! tip "[Test Data: Skyrim 202X 8.6 Update](https://www.nexusmods.com/Core/Libs/Common/Widgets/DownloadPopUp?id=249831&game_id=1704)"

### LZ4 Only

!!! info "Packing Speed with LZ4 Only, Compression Level 12"

.NET 7 & 8 Preview 3

| Thread Count | Speed         |
|--------------|---------------|
| 1            | ~32.00 MiB/s  |
| 2            | ~52.94 MiB/s  |
| 3            | ~82.22 MiB/s  |
| 4            | ~102.72 MiB/s |
| 8            | ~192.76 MiB/s |
| 12           | ~256.30 MiB/s |
| 24           | ~346.98 MiB/s |

.NET 8 Preview 3 NativeAOT

| Thread Count | Speed        |
|--------------|--------------|
| 1            | ~9.44 MiB/s  |
| 2            | ~13.78 MiB/s |
| 3            | ~18.65 MiB/s |
| 4            | ~22.52 MiB/s |
| 8            | ~37.54 MiB/s |
| 12           | ~46.88 MiB/s |
| 24           | ~59.72 MiB/s |

### ZStandard Only

!!! info "Packing Speed with ZStd Only, Compression Level 16"

!!! note "Native ZStd library used, speeds are within margin of error on all runtimes."

| Thread Count | Speed         |
|--------------|---------------|
| 1            | ~31.21 MiB/s  |
| 2            | ~53.87 MiB/s  |
| 3            | ~82.14 MiB/s  |
| 4            | ~104.73 MiB/s |
| 8            | ~187.78 MiB/s |
| 12           | ~253.03 MiB/s |
| 24           | ~331.85 MiB/s |

## Extraction Speed

!!! note "All benchmarks under this section are in-memory; due to exceeding speeds achievable by consumer grade NVMe."

!!! tip "Due to the layout of archive format; the nature of test data (i.e. many small files vs big files) has no effect on performance. (Outside of the standard FileSystem/Storage inefficiencies if writing many small files to disk)."

!!! tip "[Test Data: Skyrim 202X 8.6 Update](https://www.nexusmods.com/Core/Libs/Common/Widgets/DownloadPopUp?id=249831&game_id=1704)"

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