using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using NexusMods.Archives.Nx.Benchmarks.Benchmarks;

namespace NexusMods.Archives.Nx.Benchmarks.Columns;

public class SizeAfterTocCompressionColumn : IColumn
{
    public const string SizeAfterCompressionFileNameFormat = "size-after-compression-{0}.{1}.{2}.{3}.txt";

    public string Id => nameof(SizeAfterTocCompressionColumn);

    public string ColumnName => "ToC Size";

    public string Legend => "Size of Compressed Data (1KB = 1024B)";

    public UnitType UnitType => UnitType.Size;

    public bool AlwaysShow => true;

    public ColumnCategory Category => ColumnCategory.Metric;

    public int PriorityInCategory => 0;

    public bool IsNumeric => true;

    public bool IsAvailable(Summary summary) => true;

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        var benchmarkName = benchmarkCase.Descriptor.WorkloadMethod.Name.ToLower();
        var parameter = benchmarkCase.Parameters.Items.FirstOrDefault(x => x.Name == "N");
        if (parameter == null)
            return "no parameter";

        var n = Convert.ToInt32(parameter.Value);
        var blockSize = benchmarkCase.Parameters.Items.FirstOrDefault(x => x.Name == nameof(ParsingTableOfContents.SolidBlockSize));
        var chunkSize = benchmarkCase.Parameters.Items.FirstOrDefault(x => x.Name == nameof(ParsingTableOfContents.ChunkSize));
        var filename = GetFileName(benchmarkName, n, Convert.ToInt32(blockSize!.Value), Convert.ToInt32(chunkSize!.Value));
        return File.Exists(filename) ? File.ReadAllText(filename) : "no file";
    }

    public static string GetFileName(string benchName, int count, int solidBlockSize, int chunkSize) =>
        string.Format(SizeAfterCompressionFileNameFormat, benchName, count, solidBlockSize, chunkSize).ToLower();

    public override string ToString() => ColumnName;
}
