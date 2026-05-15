# Quick Start

## CLI

```bash
# Default — top 500 sites, recursive, with ID extraction
maigret johndoe

# Cap to a small slice
maigret johndoe --top-sites 50

# Filter by tags
maigret johndoe --tags social,us

# Skip recursion and extraction (fastest path)
maigret johndoe --no-recursion --no-extracting

# Multiple usernames + permutations (joins with _ - . separators)
maigret alice bob --permute

# Run through Tor
maigret johndoe --proxy socks5://127.0.0.1:9050

# Search by VK id
maigret 123456789 --id-type vk_id

# Persist reports to disk
maigret johndoe --txt --csv --json --markdown --html -o ./reports
```

See [[CLI Reference]] for the full flag inventory.

## Library — single call, full result list

```csharp
using Maigret.Net.Core;

var client = new MaigretClient();
var summary = await client.SearchAsync("johndoe", new MaigretSearchOptions
{
    TopSites = 50,
    Tags     = new[] { "social", "us" },
});

Console.WriteLine($"{summary.FoundCount}/{summary.TotalChecked} hits in {summary.Elapsed:mm\\:ss}");
foreach (var hit in summary.ClaimedSites)
{
    Console.WriteLine($"[+] {hit.SiteName}: {hit.SiteUrlUser}");
}
```

`MaigretSearchSummary` exposes ready-made views:

- `ClaimedSites` — username found
- `AvailableSites` — username explicitly missing
- `Errors` — captcha, bot protection, timeouts
- `Skipped` — disabled sites, wrong id type, illegal username format
- `ClaimedByUsername` — handy when running with `Permute` / `AdditionalUsernames`

## Library — streaming for live UIs

```csharp
using Maigret.Net.Core;

var client = new MaigretClient();
await foreach (var r in client.StreamAsync("johndoe", new MaigretSearchOptions { TopSites = 100 }))
{
    if (r.Status == MaigretCheckStatus.Claimed)
        Console.WriteLine($"[+] {r.SiteName}: {r.SiteUrlUser}");
}
```

See [[Library Usage]] for advanced scenarios (custom activators, ID extractors, reports).
