namespace Maigret.Net.Core.Activators;

/// <summary>
/// Default no-op activation provider used when nothing is registered.
/// </summary>
public sealed class NullActivationProvider : IActivationProvider
{
    public static readonly NullActivationProvider Instance = new();

    public bool CanActivate(MaigretSite site) => false;

    public Task ActivateAsync(MaigretSite site, string? probedUrl, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
