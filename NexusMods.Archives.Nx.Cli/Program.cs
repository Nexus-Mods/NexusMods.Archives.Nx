// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs;
using Spectre.Console;

// Common Options
var defaultPackerSettings = new PackerSettings() { Output = null! };
var maxNumThreads = new Option<int?>("--threads", () => defaultPackerSettings.MaxNumThreads, "Number of threads to spawn (values <= 0 mean default).");

// Extract Command
var extractCommand = new Command("extract", "Extract files from an archive")
{
    new Option<string>("--source", "Source archive to extract from.") { IsRequired = true },
    new Option<string>("--target", "Target location to extract to.") { IsRequired = true },
    maxNumThreads
};

extractCommand.Handler = CommandHandler.Create<string, string, int?>(Extract);

// Extract Command
var benchmarkCommand = new Command("benchmark", "Extracts a file in-memory, benchmarking the operation. Make sure you have enough RAM for 2 copies!")
{
    new Option<string>("--source", "Archive to benchmark extracting.") { IsRequired = true },
    new Option<int?>("--attempts", () => 25, "Number of decompression operations to do."),
    maxNumThreads
};

benchmarkCommand.Handler = CommandHandler.Create<string, int?, int?>(Benchmark);

// Pack Command
var packCommand = new Command("pack", "Pack files to an archive.")
{
    new Option<string>("--source", "[Required] Source folder to pack files from.") { IsRequired = true },
    new Option<string>("--target", "[Required] Target location to place packed archive to.") { IsRequired = true },
    new Option<int?>("--blocksize", () => defaultPackerSettings.BlockSize,
        "Size of SOLID blocks. Range is 4096 to 67108863 (64 MiB). This is a power of 2 (minus one) and must be smaller than chunk size."),
    new Option<int?>("--chunksize", () => defaultPackerSettings.ChunkSize, "Size of large file chunks. Range is 32768 (32K) to 1073741824 (1GiB). Must be power of 2."),
    new Option<int?>("--solidlevel", () => defaultPackerSettings.SolidCompressionLevel, "Compression level to use for SOLID data. ZStandard has Range -5 - 22. LZ4 has Range: 1 - 12."),
    new Option<int?>("--chunkedlevel", () => defaultPackerSettings.ChunkedCompressionLevel, "Compression level to use for chunks of large data. ZStandard has Range -5 - 22. LZ4 has Range: 1 - 12."),
    new Option<CompressionPreference?>("--solid-algorithm", () => defaultPackerSettings.SolidBlockAlgorithm, "Compression algorithm used for compressing SOLID blocks."),
    new Option<CompressionPreference?>("--chunked-algorithm", () => defaultPackerSettings.ChunkedFileAlgorithm, "Compression algorithm used for compressing chunked files."),
    new Option<bool>("--deduplicate-chunked", () => false, "Enable CHUNKED block file deduplication during packing."),
    new Option<bool>("--deduplicate-solid", () => true, "Enable SOLID block file deduplication during packing."),
    maxNumThreads
};

packCommand.Handler = CommandHandler.Create<string, string, int?, int?, int?, int?, CompressionPreference?, CompressionPreference?, int?, bool, bool>(Pack);

// Merge Command
var mergeCommand = new Command("merge", "Merge multiple .nx archives into a single archive")
{
    new Option<string[]>("--sources", "Source .nx archives to merge.") { IsRequired = true, AllowMultipleArgumentsPerToken = true },
    new Option<string>("--output", "Output path for the merged archive.") { IsRequired = true },
    new Option<bool>("--deduplicate-chunked", () => true, "Enable CHUNKED block file deduplication during merging."),
    new Option<bool>("--deduplicate-solid", () => true, "Enable SOLID block file deduplication during merging."),
    maxNumThreads
};

mergeCommand.Handler = CommandHandler.Create<string[], string, int?, bool, bool>(Merge);

// Root command
var rootCommand = new RootCommand
{
    extractCommand,
    packCommand,
    benchmarkCommand,
    mergeCommand
};

// Parse the incoming args and invoke the handler
rootCommand.Invoke(args);

void Extract(string source, string target, int? threads)
{
    Console.WriteLine($"Extracting {source} to {target} with [{threads}] threads.");
    var initializeTimeTaken = Stopwatch.StartNew();
    using var originalArchiveStream = new FileStream(source, FileMode.Open, FileAccess.Read);
    var provider = new FromStreamProvider(originalArchiveStream);
    var builder = new NxUnpackerBuilder(provider);
    builder.AddFilesWithDiskOutput(builder.GetFileEntriesRaw(), target);

    if (threads.HasValue)
        builder.WithMaxNumThreads(threads.Value);

    Console.WriteLine("Initialized in {0}ms", initializeTimeTaken.ElapsedMilliseconds);

    // Progress Reporting.
    var unpackingTimeTaken = Stopwatch.StartNew();
    AnsiConsole.Progress()
        .Start(ctx =>
        {
            // Define tasks
            var packTask = ctx.AddTask("[green]Unpacking Files[/]");
            var progress = new Progress<double>(d => packTask.Value = d * 100);
            builder.WithProgress(progress);
            builder.Extract();
        });

    Console.WriteLine("Unpacked in {0}ms", unpackingTimeTaken.ElapsedMilliseconds);
}

void Pack(string source, string target, int? blocksize, int? chunksize, int? solidLevel, int? chunkedLevel, CompressionPreference? solidAlgorithm, CompressionPreference? chunkedAlgorithm, int? threads, bool deduplicateChunked, bool deduplicateSolid)
{
    Console.WriteLine($"Packing {source} to {target} with {threads} threads, blocksize [{blocksize}], chunksize [{chunksize}], solidLevel [{solidLevel}], chunkedLevel [{chunkedLevel}], solidAlgorithm [{solidAlgorithm}], chunkedAlgorithm [{chunkedAlgorithm}], deduplicate [chunked: {deduplicateChunked} solid: {deduplicateSolid}].");

    var builder = new NxPackerBuilder();
    builder.AddFolder(source);
    builder.WithOutput(new FileStream(target, FileMode.Create, FileAccess.ReadWrite));

    if (blocksize.HasValue)
        builder.WithBlockSize(blocksize.Value);

    if (chunksize.HasValue)
        builder.WithChunkSize(chunksize.Value);

    if (solidLevel.HasValue)
        builder.WithSolidCompressionLevel(solidLevel.Value);

    if (chunkedLevel.HasValue)
        builder.WithChunkedLevel(chunkedLevel.Value);

    if (solidAlgorithm.HasValue)
        builder.WithSolidBlockAlgorithm(solidAlgorithm.Value);

    if (chunkedAlgorithm.HasValue)
        builder.WithChunkedFileAlgorithm(chunkedAlgorithm.Value);

    if (threads.HasValue)
        builder.WithMaxNumThreads(threads.Value);

    builder.WithChunkedDeduplication(deduplicateChunked);
    builder.WithSolidDeduplication(deduplicateSolid);

    var packingTimeTaken = Stopwatch.StartNew();

    // Progress Reporting.
    AnsiConsole.Progress()
        .Start(ctx =>
        {
            // Define tasks
            var packTask = ctx.AddTask("[green]Packing Files[/]");
            var progress = new Progress<double>(d => packTask.Value = d * 100);
            builder.WithProgress(progress);
            builder.Build(false);
        });

    var ms = packingTimeTaken.ElapsedMilliseconds;
    Console.WriteLine("Packed in {0}ms", ms);
    Console.WriteLine("Throughput {0:###.00}MiB/s", builder.Files.Sum(x => x.FileSize) / (float)ms / 1024F);
    Console.WriteLine("Size {0:F2} MiB", BytesToMiB((ulong)builder.Settings.Output.Length));
    builder.Settings.Output.Dispose();
}

void Benchmark(string source, int? threads, int? attempts)
{
    var data = File.ReadAllBytes(source);
    var builder = new NxUnpackerBuilder(new FromArrayProvider { Data = data });
    builder.AddFilesWithArrayOutput(builder.GetFileEntriesRaw(), out var outputs);
    if (threads.HasValue)
        builder.WithMaxNumThreads(threads.Value);

    long totalTimeTaken = 0;

    // Warmup, get that JIT to promote all the way to max tier.
    // With .NET 8, and R2R, this might take 2 (* 40) executions.
    for (var x = 0; x < 80; x++)
    {
        var unpackingTimeTaken = Stopwatch.StartNew();
        builder.Extract();
        Console.WriteLine("[Warmup] Unpacked in {0}ms", unpackingTimeTaken.ElapsedMilliseconds);
    }

    attempts = attempts.GetValueOrDefault(25);
    for (var x = 0; x < attempts; x++)
    {
        var unpackingTimeTaken = Stopwatch.StartNew();
        builder.Extract();
        Console.WriteLine("Unpacked in {0}ms", unpackingTimeTaken.ElapsedMilliseconds);
        totalTimeTaken += unpackingTimeTaken.ElapsedMilliseconds;
    }

    var averageMs = (totalTimeTaken / (float)attempts);
    Console.WriteLine("Average {0:###.00}ms", averageMs);
    Console.WriteLine("Throughput {0:###.00}GiB/s", outputs.Sum(x => (long)x.Data.Length) / averageMs / 1048576F);
}

void Merge(string[] sources, string output, int? threads, bool deduplicateChunked, bool deduplicateSolid)
{
    Console.WriteLine($"Merging {sources.Length} archives into {output} with [{threads}] threads [deduplicate-chunked: {deduplicateChunked}, deduplicate-solid: {deduplicateSolid}].");

    var builder = new NxDeduplicatingRepackerBuilder();
    ulong totalInputSize = 0;
    ulong totalDecompressedSize = 0;

    if (threads.HasValue)
        builder.WithMaxNumThreads(threads.Value);

    builder.WithChunkedDeduplication(deduplicateChunked);
    builder.WithSolidDeduplication(deduplicateSolid);

    foreach (var source in sources)
    {
        var provider = new FromFilePathProvider { FilePath = source };
        var fileInfo = new FileInfo(source);

        using var fileData = provider.GetFileData(0, (ulong)fileInfo.Length);
        totalInputSize += fileData.DataLength;
        var header = HeaderParser.ParseHeader(provider);
        builder.AddFilesFromNxArchive(provider, header, header.Entries.AsSpan());

        foreach (var entry in header.Entries)
            totalDecompressedSize += entry.DecompressedSize;
    }

    var mergeTimeTaken = Stopwatch.StartNew();

    // Progress Reporting.
    AnsiConsole.Progress()
        .Start(ctx =>
        {
            var mergeTask = ctx.AddTask("[green]Merging Archives[/]");
            var progress = new Progress<double>(d => mergeTask.Value = d * 100);
            builder.WithProgress(progress);
            builder.WithOutput(File.Create(output));
            builder.Build(false);
        });

    var outputSize = builder.Settings.Output.Length;
    var ms = mergeTimeTaken.ElapsedMilliseconds;
    Console.WriteLine("Merged in {0}ms", ms);
    Console.WriteLine("Input Size: {0:F2} MiB", BytesToMiB(totalInputSize));
    Console.WriteLine("Output Size: {0:F2} MiB", BytesToMiB((ulong)outputSize));
    Console.WriteLine("Compression Ratio: {0:P2}", (double)outputSize / totalInputSize);
    Console.WriteLine("Throughput {0:###.00}MiB/s", totalDecompressedSize / (float)ms / 1024F);
}

static float BytesToMiB(ulong bytes) => bytes / 1024F / 1024F;
