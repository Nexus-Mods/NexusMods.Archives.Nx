using BenchmarkDotNet.Attributes;
using NexusMods.Archives.Nx.Benchmarks.Utilities;
using NexusMods.Archives.Nx.Headers;
using static NexusMods.Archives.Nx.Benchmarks.Columns.SizeAfterCompressionColumn;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace NexusMods.Archives.Nx.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[InProcess] // Do not remove, used for size reporting.
public class CreatingStringPool
{
    public int LastSize;

    public StringWrapper[] Strings = null!;

    // Do not rename. Column depends on this name.
    [Params(1000, 2000, 4000)] public int N { get; set; }

    [GlobalSetup]
    public void Setup() => Strings = StringWrapper.FromStringArray(Assets.GetYakuzaFileList()[..N].ToArray());

    // Size Reporting 
    [GlobalCleanup(Target = nameof(Pack_LexicoGraphic))]
    public void Cleanup_LexicoGraphic() => File.WriteAllText(
        string.Format(SizeAfterCompressionFileNameFormat, nameof(Pack_LexicoGraphic), N),
        LastSize.ToString());

    [Benchmark]
    public int Pack_LexicoGraphic()
    {
        using var slice = StringPool.Pack(Strings.AsSpan());
        LastSize = slice.Length;
        return LastSize;
    }
}
