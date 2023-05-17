using FluentAssertions;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Tests.Tests.Pooling;

public class PackerPoolTests
{
    [Fact]
    public void Rent_Return_Baseline()
    {
        using var pool = new PackerArrayPool(4, 4096);
        using var rental = pool.Rent(1024);

        rental.Array.Should().NotBeNull();
        rental.Length.Should().Be(1024);
        rental.Owner.Should().Be(pool);
        rental.ArrayIndex.Should().Be(-1); // Shared Pool
    }

    [Fact]
    public void Rent_ViaArrayPool_WhenBelowThreshold()
    {
        using var pool = new PackerArrayPool(4, 2097152);
        using var rental = pool.Rent(524288);
        rental.ArrayIndex.Should().Be(-1); // Shared Pool
    }

    [Fact]
    public void BlockSize_UnderThreshold_ArraysNotAllocated()
    {
        using var pool = new PackerArrayPool(4, 524288);
        pool.HasArrays.Should().BeFalse();
    }

    [Fact]
    public void Rent_ViaArrays_WhenAboveThreshold()
    {
        using var pool = new PackerArrayPool(4, 2097152);
        using var rental = pool.Rent(1048576 + 1);
        rental.ArrayIndex.Should().BeGreaterThan(-1); // Array Pool.
    }

    /// <summary>
    ///     This only holds true in a single-threaded scenario, but interesting nonetheless.
    /// </summary>
    [Fact]
    public void Rent_Returns_SameArray_OnDisposal()
    {
        using var pool = new PackerArrayPool(1, 4096);
        byte[] rentedArray;

        using (var rental = pool.Rent(1024))
            rentedArray = rental.Array;

        using (var rental = pool.Rent(1024))
            rental.Array.Should().Equal(rentedArray);
    }

    [Fact]
    public void Rent_Throws_Exception_When_OutOfItems()
    {
        var pool = new PackerArrayPool(1, 2097152);
        using var rental = pool.Rent(1048576 + 1);
        Assert.Throws<OutOfPackerPoolArraysException>(() => pool.Rent(1048576 + 1));
    }
}
