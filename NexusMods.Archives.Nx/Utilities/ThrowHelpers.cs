using NexusMods.Archives.Nx.Headers;

namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
///     Utilities for high performance exception throwing.
/// </summary>
internal class ThrowHelpers
{
    public static void ThrowInsufficientStringPoolSizeException() => throw new InsufficientStringPoolSizeException(
        $"Size of string pool, exceeds maximum allowable ({StringPool.MaxUncompressedSize}).");

    public static void ThrowInsufficientStringPoolSizeException(nint poolSize) =>
        throw new InsufficientStringPoolSizeException(
            $"Size of string pool: {poolSize}, exceeds maximum allowable ({StringPool.MaxUncompressedSize}).");
}

/// <summary>
///     This exception is thrown when the <see cref="StringPool" /> cannot be serialized due to the sum of all the
///     uncompressed relative paths
///     being too great.
/// </summary>
public class InsufficientStringPoolSizeException : Exception
{
    /// <inheritdoc />
    public InsufficientStringPoolSizeException(string? message) : base(message) { }
}
