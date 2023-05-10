namespace NexusMods.Archives.Nx.Traits;

/// <summary>
///     For structures that can convert to Little Endian.
/// </summary>
public interface ICanConvertToLittleEndian
{
    /// <summary>
    ///     Reverses the endian of the data (on a big endian machine, if required).
    /// </summary>
    /// <remarks>
    ///     Only call this method once, or endian will be reversed again.
    /// </remarks>
    // ReSharper disable once UnusedMemberInSuper.Global
    public void ReverseEndianIfNeeded();

    /*
        Methods implementing this interface are defined as:
        
            if (BitConverter.IsLittleEndian)
                return;

            ReverseEndian(); 
            
        The `BitConverter.IsLittleEndian` is evaluated at compile-time (JIT-time) and
        no-op on Little Endian machines.
    */
}
