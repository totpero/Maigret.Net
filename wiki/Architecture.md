# Architecture

Maigret.Net ships four NuGet packages, each scoped to a single responsibility, plus a CLI tool that wires them together.

## Package layout

| Package | Folder | Responsibility |
|---|---|---|
| `Maigret.Net` (Core) | `src/Maigret.Net.Core` | Site model, HTTP checkers, search orchestration, recursive search, activation strategies, ID extraction |
| `Maigret.Net.Reports` | `src/Maigret.Net.Reports` | `IReportWriter`, `ITemplateEngine`, built-in TXT / CSV / JSON / Markdown writers, `ReportPipeline` |
| `Maigret.Net.Reports.Scriban` | `src/Maigret.Net.Reports.Scriban` | Scriban-based `ITemplateEngine` + the bundled default HTML report template |
| `Maigret.Net.Cli` | `src/Maigret.Net.Cli` | `dotnet tool` (`maigret` command) — Spectre.Console.Cli front-end |

## Core folder structure

```
Maigret.Net.Core/
├── Activators/        Twitter / Vimeo / OnlyFans activators + ISiteActivator dispatcher
├── Checkers/          SimpleHttp / Proxied / DomainResolver / Mock / CurlCffi (P2 stub)
├── Constants/         CheckTypes, HttpStatusBoundaries, IdTypes, ActivationKeys, HeaderNames
├── Errors/            CheckError + CommonErrors fingerprint table + ErrorStat
├── IdExtraction/      RuleBasedIdExtractor + BuiltInExtractionRules
├── Notify/            QueryNotify base + QueryNotifyConsole
├── Permutator/        Permute<T> + PermuteMode
├── RecursiveSearch/   IRecursiveSearchEngine + RecursiveSearchEngine
├── Results/           MaigretCheckResult + MaigretCheckStatus
├── Search/            MaigretSearchEngine + Checking + MaigretSearchRequest + SearchFilter
├── Sites/             MaigretEngine + MaigretSite + MaigretDatabase
├── Utils/             CaseConverter + UrlMatcher + TagUtils + MaigretUtilities
├── Resources/         data.json + settings.json (embedded)
└── (root)             MaigretClient, MaigretSearchOptions, MaigretSearchSummary,
                       MaigretFactory, MaigretResources, MaigretServiceCollectionExtensions,
                       Settings, QueryOptions, Executors
```

Each folder maps to its own sub-namespace (`Maigret.Net.Core.Sites`, `Maigret.Net.Core.Search`, …), and consumers pull them in through `GlobalUsings.cs`.

## Search pipeline (one-username view)

1. **`MaigretClient.SearchAsync`** builds `MaigretSearchRequest` from `MaigretSearchOptions`.
2. **`MaigretSearchEngine.SearchAsync`** resolves the request:
   - Filters the database (`MaigretDatabase.RankedSitesDict`) by tags, names, top-N, disabled flag, id-type.
   - Builds the default checker (`SimpleHttpChecker` or `ProxiedHttpChecker`).
   - Schedules one worker `Task` per `(site, username)` and pumps results into a bounded `Channel<MaigretCheckResult>`.
3. **`Checking.CheckSiteForUsernameAsync`** is the per-site pipeline:
   - Validates preconditions (disabled, id type, regex, bad chars).
   - Issues the probe through the resolved `ICheckerBase`.
   - If the site declares activation marks and they appear in the body, calls `IActivationProvider.ActivateAsync` and re-probes.
   - Interprets the response (`status_code` / `message` / `response_url`).
   - Optionally runs `IIdExtractor.Extract` for claimed accounts.
4. **`RecursiveSearchEngine`** (when enabled) harvests new IDs from claimed results, deduplicates against a `(id, type)` set, and re-enters `MaigretSearchEngine.SearchAsync` for the next depth level.

```
MaigretClient
    └─ MaigretSearchEngine.SearchAsync
        ├─ MaigretDatabase.RankedSitesDict       (site filtering)
        ├─ Channel + SemaphoreSlim               (fan-out)
        ├─ Checking.CheckSiteForUsernameAsync    (per-site)
        │   ├─ ICheckerBase.CheckAsync           (HTTP/DNS probe)
        │   ├─ IActivationProvider.ActivateAsync (optional)
        │   ├─ Checking.DetectErrorPage          (Cloudflare / 5xx / 999)
        │   ├─ Checking.InterpretResponse        (Claimed/Available/Unknown/Illegal)
        │   └─ IIdExtractor.Extract              (optional)
        └─ IRecursiveSearchEngine.SearchAsync    (optional, depth-limited)
```

## Concurrency primitives

- `Channel<MaigretCheckResult>` — bounded producer/consumer between workers and the `await foreach` consumer.
- `SemaphoreSlim(MaxConnections)` — caps in-flight HTTP requests.
- `Task.Run` per `(site, username)` — pre-scheduled before the channel pump starts.
- Cancellation propagates from `CancellationToken` to every worker, the channel reader, and the disposal pump.

## Settings vs. options

Two configuration types:
- **`Settings`** (in `Settings.cs`) — persistable JSON, mirrors `resources/settings.json`. Use it for long-lived defaults.
- **`MaigretSearchOptions`** (in `MaigretSearchOptions.cs`) — per-call runtime tweaks, modeled after the CLI flag set. `MaigretClient` translates options into a fresh `Settings` + `SearchFilter` for each call.

## Extension points

- **`ICheckerBase`** — plug a new transport (Curl-impersonate, headless browser, …).
- **`ISiteActivator`** + `IActivationProvider` — implement new dynamic-token strategies.
- **`IIdExtractor`** + `ExtractionRule` — add custom profile-parsing rules.
- **`IReportWriter`** — add a new report format.
- **`ITemplateEngine`** — swap Scriban for Razor / Liquid / Handlebars.
- **`IRecursiveSearchEngine`** — replace the depth-limited queue with your own policy.

## Multi-target frameworks

`net8.0` / `net9.0` / `net10.0`. CI runs the test suite against all three on Ubuntu / Windows / macOS.
