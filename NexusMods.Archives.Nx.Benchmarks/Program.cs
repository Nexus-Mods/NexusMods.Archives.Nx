// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using NexusMods.Archives.Nx.Benchmarks.Benchmarks;
using NexusMods.Archives.Nx.Benchmarks.Columns;

//BenchmarkRunner.Run<PathSorting>();
//BenchmarkRunner.Run<CreatingStringPool>(DefaultConfig.Instance.AddColumn(new SizeAfterCompressionColumn()));
BenchmarkRunner.Run<UnpackingStringPool>();
Console.WriteLine("Hello, World!");
