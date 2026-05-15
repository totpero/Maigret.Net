# Custom Activators

Some sites refuse anonymous probes — they require a short-lived authentication token (guest token, JWT, signed request). Maigret.Net ships activators for Twitter / X, Vimeo, and OnlyFans, and exposes a clean extension point so you can add your own.

## The contract

```csharp
public interface ISiteActivator
{
    string Method { get; }
    Task ActivateAsync(MaigretSite site, string? probedUrl, CancellationToken cancellationToken = default);
}
```

- `Method` matches the value of `activation.method` in `data.json` (case-insensitive).
- `ActivateAsync` is expected to mutate `site.Headers` with whatever token / signature is required, then return. The checker re-issues the probe with the refreshed headers automatically.

The dispatcher (`MethodBasedActivationProvider`) routes each `(site, response)` to the matching activator when the response body contains one of the site's `activation.marks` substrings.

## Built-ins

| Activator | Method name | What it does |
|---|---|---|
| `TwitterGuestTokenActivator` | `twitter` | `POST` to `activation.url`, reads `activation.src` from the JSON body, writes `x-guest-token` header |
| `VimeoJwtActivator` | `vimeo` | `GET` activation URL, reads `jwt`, writes `Authorization: jwt <token>` |
| `OnlyFansSignatureActivator` | `onlyfans` | SHA-1-signs `static_param / time / path / user-id`, sets `time` + `sign` + bootstrap cookie + `x-bc` |

All three are registered together with:

```csharp
services.AddMaigretActivators();
```

## Adding a new activator

Implement `ISiteActivator` and register it as a singleton — `MethodBasedActivationProvider` picks it up automatically.

```csharp
public sealed class HmacBearerActivator : ISiteActivator
{
    private readonly HttpClient _http;

    public HmacBearerActivator(HttpClient http) => _http = http;

    public string Method => "hmac-bearer";

    public async Task ActivateAsync(MaigretSite site, string? probedUrl, CancellationToken ct = default)
    {
        var url = site.Activation.GetProperty(ActivationKeys.Url).GetString();
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadAsStringAsync(ct);
        site.Headers[HeaderNames.Authorization] = $"Bearer {token.Trim()}";
    }
}
```

DI registration:

```csharp
services.AddMaigretActivators();
services.AddHttpClient<HmacBearerActivator>();
services.AddSingleton<ISiteActivator>(sp => sp.GetRequiredService<HmacBearerActivator>());
```

To trigger your activator, add a matching block to your site's `data.json` entry:

```json
"MyForum": {
  "activation": {
    "method": "hmac-bearer",
    "url": "https://api.example.com/auth/anon-token",
    "marks": ["please log in"]
  }
}
```

## Header conventions

Header keys are constants in `HeaderNames`. Prefer them over inline strings:

```csharp
site.Headers[HeaderNames.Authorization] = "Bearer …";
site.Headers[HeaderNames.TwitterGuestToken] = "…";
```

This keeps the activators consistent with the rest of the pipeline and surfaces typos at compile time.

## Caching

Activators are singletons. The default implementations re-fetch tokens every time the site is re-probed because data.json's activator design assumes short-lived tokens (~15 min for Twitter, ~5 min for Vimeo). If you need caching, gate the fetch with a `SemaphoreSlim` + a private dictionary keyed by `site.Name` inside your activator.

## Disabling activation

Pass `Activation = NullActivationProvider.Instance` when constructing a `MaigretClient`, or simply do not call `AddMaigretActivators()` in your DI setup. Sites with activation blocks will still be probed — they just will not get a token refresh and likely return `Unknown` (captcha) once their stale token expires.
