// Dispatcher that picks an ISiteActivator based on the site's activation.method.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maigret.Net.Core.Activators;

/// <summary>
/// Default <see cref="IActivationProvider"/> implementation. Resolves the
/// matching <see cref="ISiteActivator"/> by reading <c>site.activation.method</c>
/// out of the parsed <c>data.json</c> and looking it up in the registered set.
/// </summary>
public sealed class MethodBasedActivationProvider(
    IEnumerable<ISiteActivator> activators,
    ILogger<MethodBasedActivationProvider>? logger = null) : IActivationProvider
{
    private readonly IReadOnlyDictionary<string, ISiteActivator> _activators = (activators ?? throw new ArgumentNullException(nameof(activators)))
        .GroupBy(a => a.Method, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);
    private readonly ILogger _logger = (ILogger?)logger ?? NullLogger.Instance;

    public bool CanActivate(MaigretSite site)
    {
        ArgumentNullException.ThrowIfNull(site);
        return TryGetMethod(site, out var method) && _activators.ContainsKey(method);
    }

    public Task ActivateAsync(MaigretSite site, string? probedUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(site);

        if (!TryGetMethod(site, out var method) || !_activators.TryGetValue(method, out var activator))
        {
            _logger.LogDebug("No activator registered for method '{Method}' (site {Site})", method, site.Name);
            return Task.CompletedTask;
        }

        return activator.ActivateAsync(site, probedUrl, cancellationToken);
    }

    private static bool TryGetMethod(MaigretSite site, out string method)
    {
        method = string.Empty;
        if (site.Activation.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!site.Activation.TryGetProperty(ActivationKeys.Method, out var prop) || prop.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        method = prop.GetString() ?? string.Empty;
        return !string.IsNullOrEmpty(method);
    }
}
