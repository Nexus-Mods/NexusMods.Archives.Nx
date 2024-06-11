using BenchmarkDotNet.Attributes;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Packing.Unpack;
using NexusMods.Archives.Nx.Structs;

namespace NexusMods.Archives.Nx.Benchmarks.Benchmarks;

public class UnpackArchiveFromDisk
{
    public string Input { get; set; } = @"C:\Users\sewer\Desktop\Temp\Skyrim Special Edition.nx";
    public NxUnpacker Unpacker { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        Unpacker = new NxUnpacker(new FromStreamProvider(new FileStream(Input, FileMode.Open)));
    }

    [Benchmark]
    public void Unpack()
    {
        Unpacker.ExtractFilesToDisk(Unpacker.GetFileEntriesRaw(), "UnpackArchiveFromDisk", new UnpackerSettings());
    }
}
