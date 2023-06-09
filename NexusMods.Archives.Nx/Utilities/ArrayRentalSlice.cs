using JetBrains.Annotations;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Represents a slice of an <see cref="ArrayRental" />.
/// </summary>
[PublicAPI]
public readonly struct ArrayRentalSlice : IDisposable
{
    /// <summary>
    ///     The underlying rental.
    /// </summary>
    public ArrayRental Rental { get; }

    /// <summary>
    ///     Returns the span for the rented slice.
    /// </summary>
    public Span<byte> Span => Rental.Array.AsSpan(0, Length);

    /// <summary>
    ///     Length of this slice.
    /// </summary>
    public int Length { get; }

    /// <summary>
    ///     Represents a slice of the array rental.
    /// </summary>
    /// <param name="rental">The underlying rental.</param>
    /// <param name="length">Length of the underlying rental.</param>
    public ArrayRentalSlice(ArrayRental rental, int length)
    {
        Rental = rental;
        Length = length;
    }

    /// <inheritdoc />
    public void Dispose() => Rental.Dispose();
}
