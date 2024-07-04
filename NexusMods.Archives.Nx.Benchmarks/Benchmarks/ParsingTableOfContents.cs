using BenchmarkDotNet.Attributes;
using NexusMods.Archives.Nx.Benchmarks.Columns;
using NexusMods.Archives.Nx.Benchmarks.Utilities;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Packing.Pack.Steps;
using NexusMods.Archives.Nx.Structs.Blocks;
using static NexusMods.Archives.Nx.Benchmarks.Columns.SizeAfterTocCompressionColumn;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace NexusMods.Archives.Nx.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[InProcess] // Do not remove, used for size reporting.
public class ParsingTableOfContents
{
    // Do not rename. Column depends on this name.
    [Params(1000, 2000, 4000)] public int N { get; set; }

    [Params(32766, 524287, 1048575)] public int SolidBlockSize { get; set; }

    [Params(1024 * 1024 * 32, 1024 * 1024 * 64, 1024 * 1024 * 128)]
    public int ChunkSize { get; set; }

    internal TableOfContentsBuilder<PackerFileForBenchmarking> Builder { get; set; } = null!;
    internal byte[] PrebuiltData { get; set; } = null!;
    internal PackerFileForBenchmarking[] Files { get; set; } = null!;

    internal List<IBlock<PackerFileForBenchmarking>> Blocks { get; set; } = null!;
    public Dictionary<string, List<PackerFileForBenchmarking>> Groups { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        var entries = Assets.GetYakuzaFileEntries();
        Files = entries.Take(N).Select(x => new PackerFileForBenchmarking(x.RelativePath, x.FileSize)).ToArray();
        CreateToc(out var blocks);
        Blocks = blocks;
    }

    // Size Reporting
    [GlobalCleanup(Target = nameof(CreateTable))]
    public void Cleanup_CreateTable()
    {
        File.WriteAllText(GetFileName(nameof(CreateTable), N, SolidBlockSize, ChunkSize), PrebuiltData.Length.ToString());
        File.WriteAllText(TocNumBlocksColumn.GetFileName(nameof(CreateTable), N, SolidBlockSize, ChunkSize), Blocks.Count.ToString());
    }

    [Benchmark]
    public int CreateTable() => CreateToc(out _);

    [Benchmark]
    public int InitTableData()
    {
        var data = InitTocData();
        return data.Count; // prevent dead code elimination.
    }

    [Benchmark]
    public void InitTable()
    {
        using var toc = InitToc();
    }

    [Benchmark]
    public unsafe TableOfContents ParseTable()
    {
        fixed (byte* dataPtr = PrebuiltData)
        {
            return TableOfContents.Deserialize<TableOfContents>(dataPtr, 0, PrebuiltData.Length, Builder.Version);
        }
    }

    private TableOfContentsBuilder<PackerFileForBenchmarking> InitToc() => new(Blocks, Files);

    private List<IBlock<PackerFileForBenchmarking>> InitTocData()
    {
        // Generate blocks.
        Groups = GroupFiles.Do(Files);
        return MakeBlocks.Do(Groups, SolidBlockSize, ChunkSize);
    }

    private unsafe int CreateToc(out List<IBlock<PackerFileForBenchmarking>> blocks)
    {
        Builder.Dispose();

        // Generate blocks.
        Groups = GroupFiles.Do(Files);
        blocks = MakeBlocks.Do(Groups, SolidBlockSize, ChunkSize);

        // Generate TOC.
        Builder = new TableOfContentsBuilder<PackerFileForBenchmarking>(blocks, Files);
        foreach (var unused in Files)
        {
            ref var _ = ref Builder.GetAndIncrementFileAtomic();
        }

        // Set block infos.
        var tocSize = Builder.CalculateTableSize();
        PrebuiltData = new byte[tocSize];
        fixed (byte* dataPtr = PrebuiltData)
        {
            return Builder.Build(dataPtr, 0, tocSize);
        }
    }
}
