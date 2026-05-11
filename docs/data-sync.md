# Refreshing `data.json` from upstream

`Maigret.Net.Core` ships with `Resources/data.json` baked in as an
`EmbeddedResource`. The source of truth is the upstream Python project, kept as
a git submodule at the repository root.

## Procedure

1. **Update the submodule pointer.**

   ```bash
   git submodule update --remote maigret
   ```

2. **Copy the refreshed file into the Core resources folder.**

   ```bash
   cp maigret/maigret/resources/data.json src/Maigret.Net.Core/Resources/data.json
   ```

3. **Run the schema test to verify every site still parses.**

   ```bash
   dotnet test tests/Maigret.Net.Core.Tests --filter "FullyQualifiedName~TestSites"
   dotnet test tests/Maigret.Net.Core.Tests --filter "FullyQualifiedName~TestData"
   ```

   `TestSites` validates loading and engine merge for every site in `data.json`.
   `TestData` enforces tag-registry hygiene and that top-50 sites declare a
   category tag.

4. **Bump the patch version** in [`Directory.Build.props`](../Directory.Build.props)
   so the release workflow publishes new packages.

5. **Commit as a focused change** — one commit for the submodule pointer, one
   for the copied `data.json` plus the version bump, with a message like
   `Sync data.json from upstream maigret <commit>`.

## When to refresh

The upstream project tends to update `data.json` weekly. Refresh when:

- A new popular site is added to upstream.
- An activation rule rotates (Twitter/X, OnlyFans signing constants change every
  1–3 weeks; symptoms include sudden `Bot protection` errors on those sites).
- A user reports false positives that match a known upstream bug fix.

## Optional: refresh templates and settings

```bash
cp maigret/maigret/resources/settings.json src/Maigret.Net.Core/Resources/settings.json
cp maigret/maigret/resources/simple_report.tpl src/Maigret.Net.Reports.Scriban/Resources/simple_report.html.scriban
# (the .tpl is Jinja2 — port any new constructs to Scriban manually)
```

The Scriban template differs from upstream because Scriban syntax is not
compatible with Jinja2 — re-port any layout changes by hand rather than copying
verbatim.
