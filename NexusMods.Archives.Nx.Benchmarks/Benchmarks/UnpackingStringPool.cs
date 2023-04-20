﻿using BenchmarkDotNet.Attributes;
using NexusMods.Archives.Nx.Benchmarks.Utilities;
using NexusMods.Archives.Nx.TOC;

namespace NexusMods.Archives.Nx.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class UnpackingStringPool
{
    // Do not rename. Column depends on this name.
    [Params(1000, 2000, 4000)] public int N { get; set; }

    public byte[] Pool = null!;

    [GlobalSetup]
    public void Setup()
    {
        var strings = StringWrapper.FromStringArray(Assets.GetYakuzaFileList()[..N].ToArray());
        
        using var slice = StringPool.Pack(strings.AsSpan());
        Pool = slice.Span.ToArray();
    }
    
    [Benchmark]
    public string[] Unpack() => StringPool.Unpack(Pool);
}
