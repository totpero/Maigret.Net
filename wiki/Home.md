# Maigret.Net

**Hunt down digital footprints by username across 1,800+ sites — recursive search, ID extraction, activation tokens, and rich reports.**

A cross-platform .NET rewrite of [Maigret](https://github.com/soxoj/maigret).

## Pages

- [[Installation]]
- [[Quick Start]]
- [[CLI Reference]]
- [[Library Usage]]
- [[Architecture]]
- [[Detection Mechanisms]]
- [[Recursive Search]]
- [[Reports]]
- [[Custom Activators]]
- [[Running Tests]]

## Highlights

- One CLI (`maigret`) and one library (`MaigretClient`) — same flags, same behaviour.
- 1,800+ sites in an embedded `data.json`; no network round-trip to bootstrap.
- Streaming `IAsyncEnumerable<MaigretCheckResult>` results or a single `MaigretSearchSummary` aggregate.
- Pluggable activation (Twitter, Vimeo, OnlyFans), ID extraction, and report engines (`ITemplateEngine` for HTML).
- Multi-target: `net8.0` / `net9.0` / `net10.0`.

## Links

- [GitHub Repository](https://github.com/totpero/Maigret.Net)
- [NuGet: Maigret.Net (Core library)](https://www.nuget.org/packages/Maigret.Net)
- [NuGet: Maigret.Net.Cli (`dotnet tool`)](https://www.nuget.org/packages/Maigret.Net.Cli)
- [NuGet: Maigret.Net.Reports](https://www.nuget.org/packages/Maigret.Net.Reports)
- [NuGet: Maigret.Net.Reports.Scriban](https://www.nuget.org/packages/Maigret.Net.Reports.Scriban)
- [Project Website](https://totpero.github.io/Maigret.Net/)
- Upstream Python project: [soxoj/maigret](https://github.com/soxoj/maigret)
