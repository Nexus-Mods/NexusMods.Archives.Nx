using FluentAssertions;
using NexusMods.Archives.Nx.Tests.Utilities;
using NexusMods.Archives.Nx.TOC;

namespace NexusMods.Archives.Nx.Tests.Tests;

public class StringSortingTests
{
    [Theory]
    [MemberData(nameof(GenerateTestDataLexicographic))]
    public void SortsLexicographically_WithSubdirectoriesFirst(StringWrapper[] files, StringWrapper[] expected)
    {
        files.AsSpan().SortLexicographically();
        files.Should().Equal(expected);
    }

    public static IEnumerable<object[]> GenerateTestDataLexicographic()
    {
        var expectedResult = new[]
        {
            "Data/Movie/Credits.sfd",
            "Data/Movie/EN/Opening.sfd",
            "Data/Sonk.bin",
            "Sonk.exe"
        };

        var expected = StringWrapper.FromStringArray(expectedResult);
        foreach (var permute in expectedResult.GetPermutations())
            yield return new object[] { StringWrapper.FromStringArray(permute), expected };
    }
}
