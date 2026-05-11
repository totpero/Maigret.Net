![Maigret.Net](https://raw.githubusercontent.com/totpero/Maigret.Net/main/icon.png)

# Maigret.Net

*Hunt down digital footprints by username across 1,800+ sites — recursive search, ID extraction, activation tokens, and rich reports.*

*A .NET port of [Maigret](https://github.com/soxoj/maigret) (MIT).*

[![.NET 8.0 | 9.0 | 10.0](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![Documentation](https://img.shields.io/badge/docs-GitHub%20Pages-brightgreen)](https://totpero.github.io/Maigret.Net/)

[![NuGet Maigret.Net](https://img.shields.io/nuget/v/Maigret.Net?label=Maigret.Net&logo=nuget)](https://www.nuget.org/packages/Maigret.Net)
[![NuGet Maigret.Net.Cli](https://img.shields.io/nuget/v/Maigret.Net.Cli?label=Maigret.Net.Cli&logo=nuget)](https://www.nuget.org/packages/Maigret.Net.Cli)
[![NuGet Maigret.Net.Reports](https://img.shields.io/nuget/v/Maigret.Net.Reports?label=Maigret.Net.Reports&logo=nuget)](https://www.nuget.org/packages/Maigret.Net.Reports)
[![NuGet Maigret.Net.Reports.Scriban](https://img.shields.io/nuget/v/Maigret.Net.Reports.Scriban?label=.Scriban&logo=nuget)](https://www.nuget.org/packages/Maigret.Net.Reports.Scriban)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Maigret.Net?label=downloads&logo=nuget)](https://www.nuget.org/packages/Maigret.Net)

---

## About

**Maigret.Net** is a cross-platform .NET rewrite of [Maigret](https://github.com/soxoj/maigret), an OSINT username-search tool. It probes 1,800+ sites in parallel, follows the same `data.json` shape as the upstream Python project, and adds the features that make Maigret stand out:

- **Recursive search** — discover usernames and IDs from claimed profiles, then re-run automatically (depth-limited).
- **ID extraction** — pluggable rule engine (default ships with rules for GitHub, Twitter/X, Reddit, Instagram, HackerNews; bring your own).
- **Activation tokens** — Twitter guest token, Vimeo JWT, OnlyFans request signing — handled out of the box.
- **Tag filtering** — restrict by category (`social`, `photo`, …) or country (`us`, `ru`, …).
- **Multi-format reports** — TXT, CSV, JSON (simple + ndjson), Markdown, HTML (Scriban template, swap your own engine via `ITemplateEngine`).
- **Streaming results** — `IAsyncEnumerable<MaigretCheckResult>` from a single line of code; built on `IHttpClientFactory`, `Channel<T>`, and `SemaphoreSlim`.

### Why .NET?

- Native cross-platform binary (Windows, Linux, macOS).
- Connection pooling and SOCKS5 / Tor support via `SocketsHttpHandler` (no separate `aiohttp-socks`).
- Strongly-typed schema parsing, no PyYAML/lxml runtime surprises.
- Clean architecture: use the CLI **or** reference the library packages individually.

---

## Installation

### Prerequisites

- [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0), [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0), or [.NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) SDK
- Cross-platform: Windows, Linux, macOS

### Install as a .NET tool (recommended)

```bash
dotnet tool install --global Maigret.Net.Cli
```

After installation:

```bash
maigret zuck
```

Update / uninstall:

```bash
dotnet tool update    --global Maigret.Net.Cli
dotnet tool uninstall --global Maigret.Net.Cli
```

> Make sure `~/.dotnet/tools` (Linux/macOS) or `%USERPROFILE%\.dotnet\tools` (Windows) is on your `PATH`.

### Use as a library

```bash
dotnet add package Maigret.Net                    # core engine
dotnet add package Maigret.Net.Reports            # TXT/CSV/JSON/Markdown writers + abstractions
dotnet add package Maigret.Net.Reports.Scriban    # default Scriban-based HTML template
```

---

## Quick start

### CLI

```bash
maigret octocat
maigret octocat --top-sites 50 --tags us,social --no-color
maigret octocat --txt --csv --json --markdown --html -o ./reports
maigret 123456789 --id-type vk_id
maigret --proxy socks5://127.0.0.1:9050 alice bob --permute
maigret --help
```

### Library — same flags as the CLI, returned as a list

```csharp
using Maigret.Net.Core;

var client = new MaigretClient();
var summary = await client.SearchAsync("johndoe", new MaigretSearchOptions
{
    TopSites      = 50,
    Tags          = new[] { "social", "us" },
    Proxy         = "socks5://127.0.0.1:9050",
    NoRecursion   = false,   // recursive ID-driven follow-ups (CLI default)
    NoExtracting  = false,   // pull profile data into MaigretCheckResult.IdsData
    Permute       = false,
    Timeout       = 30,
    MaxConnections = 100,
});

Console.WriteLine($"{summary.FoundCount}/{summary.TotalChecked} hits in {summary.Elapsed:mm\\:ss}");
foreach (var hit in summary.ClaimedSites)
{
    Console.WriteLine($"[+] {hit.SiteName}: {hit.SiteUrlUser}");
}
```

### Library — streaming (live results)

```csharp
using Maigret.Net.Core;

var client = new MaigretClient();
await foreach (var r in client.StreamAsync("johndoe"))
{
    if (r.Status == MaigretCheckStatus.Claimed)
        Console.WriteLine($"[+] {r.SiteName}: {r.SiteUrlUser}");
}
```

### Library — DI + reports

```csharp
using Maigret.Net.Core;
using Maigret.Net.Core.Activators;
using Maigret.Net.Core.IdExtraction;
using Maigret.Net.Core.RecursiveSearch;
using Maigret.Net.Reports;
using Maigret.Net.Reports.Scriban;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddLogging();
services.AddMaigret(opt => { opt.MaxConnections = 50; opt.TopSitesCount = 200; });
services.AddMaigretActivators();        // Twitter / Vimeo / OnlyFans
services.AddMaigretIdExtraction();      // built-in rules + your own ExtractionRule singletons
services.AddMaigretRecursiveSearch();
services.AddMaigretReports();           // TXT / CSV / JSON / Markdown
services.AddMaigretScribanReports();    // HTML

var sp       = services.BuildServiceProvider();
var engine   = sp.GetRequiredService<IRecursiveSearchEngine>();
var pipeline = sp.GetRequiredService<ReportPipeline>();
var settings = sp.GetRequiredService<Settings>();
var db       = sp.GetRequiredService<MaigretDatabase>();

var results = new List<MaigretCheckResult>();
await foreach (var r in engine.SearchAsync(new[] { "octocat" }, db, settings))
    results.Add(r);

var ctx = new ReportContext("octocat", results);
await pipeline.WriteToFolderAsync("./reports", ctx, new[] { "txt", "csv", "json", "markdown", "html" });
```

### Custom template engine (Razor / Liquid / …)

```csharp
public sealed class MyEngine : ITemplateEngine
{
    public string EngineId => "my-engine";
    public Task<string> RenderAsync(string template, object model, CancellationToken ct = default) => /* ... */;
}

services.AddSingleton<ITemplateEngine, MyEngine>();
services.AddSingleton<IReportWriter>(sp =>
    new HtmlReportWriter(sp.GetRequiredService<ITemplateEngine>(), File.ReadAllText("my-template.html")));
```

---

## Project layout

```
Maigret.Net.slnx
├─ src/
│  ├─ Maigret.Net.Core/              → Maigret.Net          (engine + sites + checkers + search + activation + extraction)
│  ├─ Maigret.Net.Reports/           → Maigret.Net.Reports          (IReportWriter, ITemplateEngine, TXT/CSV/JSON/Markdown)
│  ├─ Maigret.Net.Reports.Scriban/   → Maigret.Net.Reports.Scriban  (default HTML template engine)
│  └─ Maigret.Net.Cli/               → Maigret.Net.Cli              (the `maigret` global tool)
└─ tests/
   ├─ Maigret.Net.Core.Tests/        (xUnit + NSubstitute + Shouldly)
   └─ Maigret.Net.Reports.Tests/
```

The Python `maigret` repository is kept as a git submodule at the repository root, used as the source of truth for `data.json`. See [`docs/data-sync.md`](docs/data-sync.md) for the periodic refresh procedure.

---

## Build from source

```bash
git clone --recurse-submodules https://github.com/totpero/Maigret.Net.git
cd Maigret.Net
dotnet build Maigret.Net.slnx
dotnet test  Maigret.Net.slnx --filter "Category!=Integration"
```

Run the CLI without installing:

```bash
dotnet run --project src/Maigret.Net.Cli -- octocat --top-sites 25
```

Pack locally:

```bash
dotnet pack Maigret.Net.slnx -c Release -o ./artifacts
dotnet tool install --global --add-source ./artifacts Maigret.Net.Cli --version 0.1.0
```

---

## Roadmap (post-MVP)

- PDF report (QuestPDF)
- XMind & Vis.js network-graph reports
- Auto-update of `data.json` from upstream (port `db_updater.py`)
- Web UI (port `web/app.py` to ASP.NET Core minimal API)
- AI analysis module (Anthropic / OpenAI)
- TLS-fingerprinting bypass for Cloudflare-strict sites (`curl_cffi` equivalent)

---

## Credits

This is a port of [soxoj/maigret](https://github.com/soxoj/maigret) (MIT) and bundles its `data.json` site database. All site definitions, detection rules, and extraction patterns originate from the upstream Python project. The `Permute<T>` permutator is a port of code originally by [balestek](https://github.com/balestek) (MIT). Solution scaffold is modelled after [totpero/Sherlock.Net](https://github.com/totpero/Sherlock.Net).

## License

[MIT](LICENSE)
