// DI registration for consumers that want Maigret available via constructor injection.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maigret.Net.Core;

/// <summary>
/// <see cref="IServiceCollection"/> extensions for wiring up Maigret services.
/// </summary>
public static class MaigretServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default <see cref="Settings"/>, embedded <see cref="MaigretDatabase"/>,
    /// no-op activation/extractor providers, and a named <see cref="HttpClient"/>.
    /// </summary>
    public static IServiceCollection AddMaigret(
        this IServiceCollection services,
        Action<Settings>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<MaigretDatabase>(_ => MaigretResources.LoadEmbeddedDatabase());
        services.TryAddSingleton<Settings>(_ =>
        {
            var s = Settings.LoadFromEmbedded();
            configure?.Invoke(s);
            return s;
        });

        services.TryAddSingleton<IActivationProvider>(_ => NullActivationProvider.Instance);
        services.TryAddSingleton<IIdExtractor>(_ => NullIdExtractor.Instance);

        services.AddHttpClient(MaigretDefaults.HttpClientName, client => client.DefaultRequestHeaders.UserAgent.ParseAdd(MaigretUtilities.GetRandomUserAgent()));

        return services;
    }
}
