namespace NexusMods.Archives.Nx.Utilities;

/// <summary>
/// Math related utilities
/// </summary>
internal static class MathUtilities
{
    /// <summary>
    /// Rounds the current value up to next multiple of 4096.
    /// </summary>
    public static long RoundUp4096(this long value) => (value + 4095) & ~4095;
    
    /// <summary>
    /// Rounds the current value up to next multiple of 4096.
    /// </summary>
    public static int RoundUp4096(this int value) => (value + 4095) & ~4095;
}
