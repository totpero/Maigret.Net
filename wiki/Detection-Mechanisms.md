# Detection Mechanisms

Every site in `data.json` declares a `checkType` that determines how Maigret.Net interprets the HTTP response. The pipeline is implemented in `Search/Checking.cs`.

## `status_code`

> **Claimed** when `200 ≤ HTTP status < 300`. **Available** otherwise.

Used by sites that return a clean `404` for missing usernames and `200` for existing ones (most REST APIs and many SPA-frontend sites).

When `requestHeadOnly` is `true` and `checkType == status_code`, the probe uses `HEAD` instead of `GET` to save bandwidth.

## `message`

> **Available** if any `absenceStrs` substring is present in the body. Otherwise **Claimed** when any `presenseStrs` substring is present.

Used by sites that always return `200` and signal user existence through page content. Notes:

- `absenceStrs` wins over `presenseStrs` — a "user not found" string takes priority even if a stray "profile" marker also appears.
- Empty `presenseStrs` is treated as "presence on any non-empty body".
- The Python project (and `data.json`) uses the typo `presense*Strs`; Maigret.Net accepts both `presense` and `presence` spellings.

## `response_url`

> **Claimed** when the response is 2xx **and** at least one `presenseStrs` mark is present.

For sites that 302-redirect to a generic landing page for missing usernames. The orchestrator turns off automatic redirects so the original status code (which the site uses to signal "claimed") is preserved.

## Error pre-filters

Before status interpretation runs, `Checking.DetectErrorPage` short-circuits to `Unknown` when:

- The body matches one of the 16 fingerprints in `CommonErrors.Fingerprints` (Cloudflare challenges, DDoS-Guard, Incapsula, etc.).
- The body matches a site-specific `errors` string from `data.json`.
- The status is `403` and the site does not opt out via `ignore403: true`.
- The status is `>= 500` (server-side outage).

`999` is treated specially as "not found" — it is LinkedIn's anti-bot code for missing profiles.

## Illegal outcomes

`Checking.CheckSiteForUsernameAsync` returns `Illegal` before any probe when:

- The site is `disabled: true` and `--all-sites` / `Forced` is not set.
- The site declares a different identifier type than the search (`type: gaia_id` vs. `username`).
- The username does not match the site's `regexCheck`.
- The username contains characters in `Checking.BadChars` (`#`).

## Activation

Some sites need a fresh authentication header before every probe. They declare an `activation` block in `data.json`:

```json
"Twitter": {
  "activation": {
    "method": "twitter",
    "url": "https://api.twitter.com/1.1/guest/activate.json",
    "src": "guest_token",
    "marks": ["unauthorized"]
  }
}
```

When the response body contains any `marks` substring, `IActivationProvider.ActivateAsync` is invoked once and the request is re-issued with refreshed headers. See [[Custom Activators]] for plugging in your own.

## Status enum

| `MaigretCheckStatus` | Meaning |
|---|---|
| `Claimed` | Username exists on the site |
| `Available` | Username explicitly does not exist |
| `Unknown` | Captcha / WAF / timeout / server error |
| `Illegal` | Skipped before probing (disabled, wrong id type, invalid format) |

`MaigretCheckResult.IsFound` is shorthand for `Status == Claimed`.
