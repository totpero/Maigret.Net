// Per-site activator contract. Implementations attach freshly fetched
// authentication material (guest token, JWT, signing headers) to
// MaigretSite.Headers before the request is re-issued.

namespace Maigret.Net.Core.Activators;

/// <summary>
/// Strategy contract for one activation method. The method name comes from
/// <c>site.activation.method</c> in <c>data.json</c> and must match
/// <see cref="Method"/> exactly (case-insensitive).
/// </summary>
public interface ISiteActivator
{
    /// <summary>Activation method name as written in <c>data.json</c>.</summary>
    public string Method { get; }

    /// <summary>
    /// Performs activation. Implementations typically mutate
    /// <see cref="MaigretSite.Headers"/>. <paramref name="probedUrl"/> is the URL
    /// that triggered activation (e.g. the OnlyFans path being signed).
    /// </summary>
    public Task ActivateAsync(MaigretSite site, string? probedUrl, CancellationToken cancellationToken = default);
}
