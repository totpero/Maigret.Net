namespace Maigret.Net.Core.Activators;

/// <summary>
/// Strategy interface for site activation. <see cref="Checking"/> calls
/// <see cref="ActivateAsync"/> when a response contains one of the marks
/// declared in the site's <c>activation</c> rule, then re-runs the request.
/// </summary>
public interface IActivationProvider
{
    /// <summary>Returns true if this provider can activate the given site.</summary>
    public bool CanActivate(MaigretSite site);

    /// <summary>
    /// Performs activation. Implementations typically mutate <paramref name="site"/>
    /// — e.g. attach a freshly fetched token to <see cref="MaigretSite.Headers"/>.
    /// </summary>
    public Task ActivateAsync(MaigretSite site, string? probedUrl, CancellationToken cancellationToken = default);
}
