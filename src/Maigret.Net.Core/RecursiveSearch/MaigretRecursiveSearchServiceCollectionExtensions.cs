// DI helpers — registers the recursive search engine.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maigret.Net.Core.RecursiveSearch;

public static class MaigretRecursiveSearchServiceCollectionExtensions
{
    /// <summary>Registers <see cref="RecursiveSearchEngine"/> as the default <see cref="IRecursiveSearchEngine"/>.</summary>
    public static IServiceCollection AddMaigretRecursiveSearch(this IServiceCollection services)
    {
        services.TryAddSingleton<IRecursiveSearchEngine, RecursiveSearchEngine>();
        return services;
    }
}
