using FluentAssertions;
using NexusMods.Archives.Nx.Packing.Pack;
using NexusMods.Archives.Nx.Tests.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Packing;

public class PackerSortingTests
{
    /// <summary>
    ///     Just an assertion at test-time 😉
    /// </summary>
    [Theory]
    [MemberData(nameof(GenerateTestData))]
    public void SortsSize_Ascending(HasFileSizeWrapper[] files, HasFileSizeWrapper[] expected)
    {
        files.SortBySizeAscending();
        files.Should().Equal(expected);
    }

    public static IEnumerable<object[]> GenerateTestData()
    {
        var expectedResult = new long[]
        {
            1,
            2,
            3,
            4
        };

        var expected = HasFileSizeWrapper.FromSizeArray(expectedResult);
        foreach (var permute in expectedResult.GetPermutations())
            yield return new object[] { HasFileSizeWrapper.FromSizeArray(permute), expected };
    }
}
