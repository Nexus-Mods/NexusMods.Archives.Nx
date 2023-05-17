# Benchmarks

!!! tip "Coming Soon (TM)"

!!! info "Spoiler: This bottlenecks any NVMe 😀"

!!! info

    All tests were executed in the following environment:  
    - `Library Version`: 0.3.0-preview  
    - `CPU`: AMD Ryzen 9 5900X  
    - `RAM`: 32GB DDR4-3000 (16-17-17-35)  
    - `OS`: Windows 11 22H2 (Build 22621)  
    - `Storage`: Samsung 980 Pro 1TB (NVMe) [PCI-E 3.0 x4]  
    - `Runtime`: .NET 8 (Preview 3) NativeAOT-x64

## Extraction Speed

!!! note "All benchmarks under this section are in-memory; due to exceeding speeds achievable by consumer grade NVMe."

!!! tip "Due to the layout of archive format; the nature of test data (i.e. many small files vs big files) has no effect on performance. (Outside of the standard FileSystem/Storage inefficiencies if writing many small files to disk)."

!!! tip "[Test Data: Skyrim 202X 8.6 Update](https://www.nexusmods.com/Core/Libs/Common/Widgets/DownloadPopUp?id=249831&game_id=1704)"

### ZStandard Only

!!! info "Decompression Speed when Extracting with ZStandard Only"

!!! tip "Files were packed with 16MB Chunk Size, and ZStd Level 16"

| Thread Count | Speed     |
|--------------|-----------|
| 1            | ~1.01 GiB |
| 2            |           |
| 3            |           |