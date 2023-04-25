using BenchmarkDotNet.Attributes;
using NexusMods.Archives.Nx.Benchmarks.Utilities;
using NexusMods.Archives.Nx.Headers;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace NexusMods.Archives.Nx.Benchmarks.Benchmarks;

public class PathSorting
{
    public StringWrapper[] Strings = null!;

    // Do not rename. Column depends on this name.
    [Params(1000, 2000, 4000)] public int N { get; set; }

    [GlobalSetup]
    public void Setup() => Strings = StringWrapper.FromStringArray(Assets.GetYakuzaFileList()[..N].ToArray());

    // Benchmarks
    [Benchmark]
    public StringWrapper[] SortPaths_LexicoGraphic()
    {
        Strings.AsSpan().SortLexicographically();
        return Strings;
    }
}
