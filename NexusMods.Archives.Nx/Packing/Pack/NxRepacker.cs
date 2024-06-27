namespace NexusMods.Archives.Nx.Packing.Pack;

/// <summary>
///     Utility that creates `.nx` archives using an existing archive
///     as base.
/// </summary>
/// <remarks>
///     The following rules are applied:
///
///     - Reused Chunked Blocks are Direct Copied (if Present)
///     - [Optional] SOLID blocks are Reused if Not Modified (no file was removed)
///     - [Optional] SOLID blocks are Reused if Not Modified (no file was added)
///
/// </remarks>
public class NxRepacker
{

}
