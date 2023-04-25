using NexusMods.Archives.Nx.Enums;

namespace NexusMods.Archives.Nx.Traits;

/// <summary>
///     Used for items which can specify a preference on whether they'd prefer to be SOLIDly packed or not.
/// </summary>
public interface IHasSolidType
{
    /// <summary>
    ///     Preference in terms of whether this item should be SOLID or not.
    /// </summary>
    public SolidPreference SolidType { get; }
}
