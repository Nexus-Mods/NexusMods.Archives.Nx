using BenchmarkDotNet.Attributes;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Benchmarks.Benchmarks;

public class PackArchiveToDisk
{
    public string Directory { get; set; } = @"C:/Users/sewer/Desktop/Sonic Heroes";
    public string OutputPath { get; set; } = @"PackArchiveToDisk.nx";
    public PackerFile[] Files { get; set; } = null!;

    [GlobalSetup]
    public void Setup() => Files = FileFinder.GetFiles(Directory).ToArray();

    [GlobalCleanup]
    public void Cleanup() => File.Delete(OutputPath);

    [Benchmark]
    public void Pack()
    {
        using var output = new FileStream(OutputPath, FileMode.Create);
        NxPacker.Pack(Files, new PackerSettings()
        {
            Output = output
        });
    }
}
