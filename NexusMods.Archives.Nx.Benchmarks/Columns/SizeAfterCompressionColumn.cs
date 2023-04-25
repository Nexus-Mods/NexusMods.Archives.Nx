using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace NexusMods.Archives.Nx.Benchmarks.Columns;

public class SizeAfterCompressionColumn : IColumn
{
    public const string SizeAfterCompressionFileNameFormat = "size-after-compression-{0}.{1}.txt";

    public string Id => nameof(SizeAfterCompressionColumn);

    public string ColumnName => "Size After Compression";

    public string Legend => "Size of Compressed Data (1KB = 1024B)";

    public UnitType UnitType => UnitType.Size;

    public bool AlwaysShow => true;

    public ColumnCategory Category => ColumnCategory.Metric;

    public int PriorityInCategory => 0;

    public bool IsNumeric => true;

    public bool IsAvailable(Summary summary) => true;

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) =>
        GetValue(summary, benchmarkCase, SummaryStyle.Default);

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        var benchmarkName = benchmarkCase.Descriptor.WorkloadMethod.Name.ToLower();
        var parameter = benchmarkCase.Parameters.Items.FirstOrDefault(x => x.Name == "N");
        if (parameter == null)
            return "no parameter";

        var n = Convert.ToInt32(parameter.Value);
        var filename = GetFileName(benchmarkName, n);
        return File.Exists(filename) ? File.ReadAllText(filename) : "no file";
    }

    public static string GetFileName(string benchName, int count) =>
        string.Format(SizeAfterCompressionFileNameFormat, benchName, count).ToLower();

    public override string ToString() => ColumnName;
}
