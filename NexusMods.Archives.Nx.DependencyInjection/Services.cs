using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Archives.Nx.Utilities;

namespace NexusMods.Archives.Nx.DependencyInjection;

/// <summary>
///     DI service helpers.
/// </summary>
[PublicAPI]
public static class Services
{
    /// <summary>
    ///     Adds services related to the Nexus Archiver.
    /// </summary>
    public static IServiceCollection AddNxArchiver(this IServiceCollection services)
    {
        return services;
    }
}
