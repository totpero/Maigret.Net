// Port of activation.ParsingActivator.twitter — fetches a Twitter/X guest token.
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maigret.Net.Core.Activators;

/// <summary>
/// Replenishes Twitter's <c>x-guest-token</c> header by POSTing to the activation
/// URL declared in <c>site.activation.url</c> and reading the field named in
/// <c>site.activation.src</c>.
/// </summary>
public sealed class TwitterGuestTokenActivator(
    HttpClient client,
    ILogger<TwitterGuestTokenActivator>? logger = null) : ISiteActivator
{
    private readonly HttpClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly ILogger _logger = (ILogger?)logger ?? NullLogger.Instance;

    public string Method => "twitter";

    public async Task ActivateAsync(MaigretSite site, string? probedUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(site);

        if (!site.Activation.TryGetProperty(ActivationKeys.Url, out var urlEl) || urlEl.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Twitter activation for {site.Name} is missing '{ActivationKeys.Url}'.");
        }

        if (!site.Activation.TryGetProperty(ActivationKeys.Src, out var srcEl) || srcEl.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Twitter activation for {site.Name} is missing '{ActivationKeys.Src}'.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, urlEl.GetString());
        foreach (var (k, v) in site.Headers)
        {
            if (k.Equals(HeaderNames.TwitterGuestToken, StringComparison.OrdinalIgnoreCase))
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

        var srcKey = srcEl.GetString()!;
        if (!doc.RootElement.TryGetProperty(srcKey, out var token) || token.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Twitter activation response for {site.Name} missing field '{srcKey}'.");
        }

        site.Headers[HeaderNames.TwitterGuestToken] = token.GetString() ?? string.Empty;
        _logger.LogDebug("Twitter guest token refreshed for {Site}", site.Name);
    }
}
