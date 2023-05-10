using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.Structs;

/// <summary>
/// Allows you to modify unpacker behaviour.
/// </summary>
public class UnpackerSettings
{
    /// <summary>
    ///     Reports progress back to the process.
    /// </summary>
    public IProgress<double>? Progress { get; set; }

    /// <summary>
    ///     Maximum number of threads allowed.
    /// </summary>
    public int MaxNumThreads { get; set; } = Environment.ProcessorCount;
    
    /// <summary>
    ///     Sanitizes settings to acceptable values if they are out of range or undefined.
    /// </summary>
    public void Sanitize() => MaxNumThreads = Polyfills.Clamp(MaxNumThreads, 1, int.MaxValue);
}
