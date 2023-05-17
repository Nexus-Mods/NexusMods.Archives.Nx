// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using NexusMods.Archives.Nx.Enums;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Packing;
using Spectre.Console;

// Common Options
var maxNumThreads = new Option<int?>("--threads", () => null, "Number of threads to spawn (values <= 0 mean default).");

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
    new Option<int?>("--attempts", () => null, "Number of decompression operations to do. (Default: 25)"),
    maxNumThreads
};

benchmarkCommand.Handler = CommandHandler.Create<string, int?, int?>(Benchmark);

// Pack Command
var packCommand = new Command("pack", "Pack files to an archive.")
{
    new Option<string>("--source", "[Required] Source folder to pack files from.") { IsRequired = true },
    new Option<string>("--target", "[Required] Target location to place packed archive to.") { IsRequired = true },
    new Option<int?>("--blocksize", () => null,
        "Size of SOLID blocks. Range is 32767 to 67108863 (64 MiB). This is a power of 2 (minus one) and must be smaller than chunk size."),
    new Option<int?>("--chunksize", () => null, "Size of large file chunks. Range is 4194304 (4 MiB) to 536870912 (512 MiB)."),
    new Option<int?>("--zstandardlevel", () => null, "Compression level to use for ZStandard if ZStandard is used. Range: 1 - 22."),
    new Option<int?>("--lz4level", () => null, "Compression level to use for LZ4 if LZ4 is used. Range: 1 - 12."),
    new Option<CompressionPreference?>("--solid-algorithm", () => null, "Compression algorithm used for compressing SOLID blocks."),
    new Option<CompressionPreference?>("--chunked-algorithm", () => null, "Compression algorithm used for compressing chunked files."),
    maxNumThreads
};

packCommand.Handler = CommandHandler.Create<string, string, int?, int?, int?, int?, CompressionPreference?, CompressionPreference?, int?>(Pack);

// Root command
var rootCommand = new RootCommand
{
    extractCommand,
    packCommand,
    benchmarkCommand
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

void Pack(string source, string target, int? blocksize, int? chunksize, int? zstandardlevel, int? lz4Level, CompressionPreference? solidAlgorithm, CompressionPreference? chunkedAlgorithm, int? threads)
{
    Console.WriteLine($"Packing {source} to {target} with {threads} threads, blocksize [{blocksize}], chunksize [{chunksize}], zstandardlevel [{zstandardlevel}], lz4level [{lz4Level}], solidAlgorithm [{solidAlgorithm}], chunkedAlgorithm [{chunkedAlgorithm}].");
    
    var builder = new NxPackerBuilder();
    builder.AddFolder(source);
    builder.WithOutput(new FileStream(target, FileMode.Create, FileAccess.ReadWrite));
    
    if (blocksize.HasValue)
        builder.WithBlockSize(blocksize.Value);
    
    if (chunksize.HasValue)
        builder.WithChunkSize(chunksize.Value);

    if (zstandardlevel.HasValue)
        builder.WithZStandardLevel(zstandardlevel.Value);
    
    if (lz4Level.HasValue)
        builder.WithLZ4Level(lz4Level.Value);
    
    if (solidAlgorithm.HasValue)
        builder.WithSolidBlockAlgorithm(solidAlgorithm.Value);
    
    if (chunkedAlgorithm.HasValue)
        builder.WithChunkedFileAlgorithm(chunkedAlgorithm.Value);
    
    if (threads.HasValue)
        builder.WithMaxNumThreads(threads.Value);

    // TODO: Implement the packing logic here.
    var packingTimeTaken = Stopwatch.StartNew();

    // Progress Reporting.
    AnsiConsole.Progress()
        .Start(ctx => 
        {
            // Define tasks
            var packTask = ctx.AddTask("[green]Packing Files[/]");
            var progress = new Progress<double>(d => packTask.Value = d * 100);
            builder.WithProgress(progress);
            builder.Build();
        });
    
    Console.WriteLine("Packed in {0}ms", packingTimeTaken.ElapsedMilliseconds);
}

void Benchmark(string source, int? threads, int? attempts)
{
    var data = File.ReadAllBytes(source);
    var builder = new NxUnpackerBuilder(new FromArrayProvider { Data = data });
    builder.AddFilesWithArrayOutput(builder.GetFileEntriesRaw(), out var outputs);
    if (threads.HasValue)
        builder.WithMaxNumThreads(threads.Value);

    long totalTimeTaken = 0;
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
    Console.WriteLine("Throughput {0:###.00}MB/s", outputs.Sum(x => (long)x.Data.Length) / averageMs / 1000);
}