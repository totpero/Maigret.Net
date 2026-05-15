# Library Usage

Install the NuGet packages you need:

```bash
dotnet add package Maigret.Net                    # core engine
dotnet add package Maigret.Net.Reports            # report writers (optional)
dotnet add package Maigret.Net.Reports.Scriban    # default HTML template (optional)
```

## Minimal: `MaigretClient`

This is the closest analog to the CLI experience — instantiate the client, pass options, await a result.

```csharp
using Maigret.Net.Core;

var client = new MaigretClient();
var summary = await client.SearchAsync("johndoe", new MaigretSearchOptions
{
    TopSites       = 50,
    Tags           = new[] { "social", "us" },
    Proxy          = "socks5://127.0.0.1:9050",
    NoRecursion    = false,
    NoExtracting   = false,
    Permute        = false,
    Timeout        = 30,
    MaxConnections = 100,
    IgnoreSites    = new[] { "Spam" },
});

Console.WriteLine($"{summary.FoundCount}/{summary.TotalChecked} hits in {summary.Elapsed:mm\\:ss}");
foreach (var hit in summary.ClaimedSites)
{
    Console.WriteLine($"[+] {hit.SiteName}: {hit.SiteUrlUser}");
    if (hit.IdsData is { } ids)
        foreach (var (k, v) in ids)
            Console.WriteLine($"     ├─ {k}: {v}");
}
```

### `MaigretSearchOptions`

Every CLI flag has a property. See [[CLI Reference]] for descriptions.

### `MaigretSearchSummary`

| Member | Notes |
|---|---|
| `Username` | First username searched |
| `Results` | Every probe, regardless of status |
| `ClaimedSites` | `Status == Claimed` |
| `AvailableSites` | `Status == Available` |
| `Errors` | `Status == Unknown` (captcha, bot protection, timeouts) |
| `Skipped` | `Status == Illegal` (disabled, wrong id type, bad format) |
| `FoundCount`, `TotalChecked`, `AnyFound`, `Elapsed` | counters |
| `ClaimedByUsername` | `IReadOnlyDictionary<string, IReadOnlyList<MaigretCheckResult>>` for multi-username runs |

## Streaming

For live UI updates or very long runs, consume results as they arrive:

```csharp
using Maigret.Net.Core;

var client = new MaigretClient();
await foreach (var r in client.StreamAsync("johndoe"))
{
    if (r.Status == MaigretCheckStatus.Claimed)
        Console.WriteLine($"[+] {r.SiteName}: {r.SiteUrlUser}");
}
```

## With DI

`AddMaigret` registers everything; opt-in to activators, ID extraction, recursion, and reports separately.

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
var client   = ActivatorUtilities.CreateInstance<MaigretClient>(sp); // or use MaigretClient directly
var pipeline = sp.GetRequiredService<ReportPipeline>();

var summary = await client.SearchAsync("johndoe");

var ctx = new ReportContext(summary.Username, summary.Results);
await pipeline.WriteToFolderAsync("./reports", ctx, new[] { "txt", "csv", "json", "markdown", "html" });
```

## Lower-level: streaming through `MaigretSearchEngine`

When you want explicit control over the request shape (e.g. plug in custom checkers):

```csharp
using Maigret.Net.Core;
using Maigret.Net.Core.Sites;
using Maigret.Net.Core.Checkers;
using Maigret.Net.Core.Search;

var database = MaigretResources.LoadEmbeddedDatabase();
var settings = Settings.LoadFromEmbedded();

var request = new MaigretSearchRequest
{
    Usernames = new[] { "johndoe" },
    Database  = database,
    Settings  = settings,
    Filter    = new SearchFilter { TopSites = 100 },
};

await foreach (var r in MaigretSearchEngine.SearchAsync(request))
{
    Console.WriteLine($"{r.SiteName}: {r.Status}");
}
```

See [[Custom Activators]] for plugging in your own token-refresh logic and [[Reports]] for swapping out the HTML template engine.
