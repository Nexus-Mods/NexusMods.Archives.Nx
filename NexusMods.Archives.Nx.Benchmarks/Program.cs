// ReSharper disable RedundantUsingDirective
// See https://aka.ms/new-console-template for more information

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using NexusMods.Archives.Nx.Benchmarks.Benchmarks;
using NexusMods.Archives.Nx.Benchmarks.Columns;

//BenchmarkRunner.Run<PathSorting>();
//BenchmarkRunner.Run<CreatingStringPool>(DefaultConfig.Instance.AddColumn(new SizeAfterCompressionColumn()));
//BenchmarkRunner.Run<UnpackingStringPool>();
//BenchmarkRunner.Run<ParsingTableOfContents>(DefaultConfig.Instance.AddColumn(new SizeAfterTocCompressionColumn()).AddColumn(new TocNumBlocksColumn()));
BenchmarkRunner.Run<OutputWriteLock>();
Console.WriteLine("Hello, World!");
