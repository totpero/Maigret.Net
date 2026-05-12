// Port of activation.ParsingActivator.vimeo — fetches the viewer JWT.
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maigret.Net.Core.Activators;

/// <summary>
/// Refreshes Vimeo's <c>Authorization</c> header by GETting the activation URL
/// and reading <c>jwt</c>. Result is stored as <c>jwt &lt;token&gt;</c>.
/// </summary>
public sealed class VimeoJwtActivator(
    HttpClient client,
    ILogger<VimeoJwtActivator>? logger = null) : ISiteActivator
{
    private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly ILogger _logger = (ILogger?)logger ?? NullLogger.Instance;

    public string Method => "vimeo";

    public async Task ActivateAsync(MaigretSite site, string? probedUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(site);

        if (!site.Activation.TryGetProperty(ActivationKeys.Url, out var urlEl) || urlEl.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Vimeo activation for {site.Name} is missing '{ActivationKeys.Url}'.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, urlEl.GetString());
        foreach (var (k, v) in site.Headers)
        {
            if (k.Equals(HeaderNames.Authorization, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            request.Headers.TryAddWithoutValidation(k, v);
        }

        using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!doc.RootElement.TryGetProperty("jwt", out var jwt) || jwt.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Vimeo activation response for {site.Name} missing 'jwt'.");
        }

        site.Headers[HeaderNames.Authorization] = "jwt " + jwt.GetString();
        _logger.LogDebug("Vimeo JWT refreshed for {Site}", site.Name);
    }
}
