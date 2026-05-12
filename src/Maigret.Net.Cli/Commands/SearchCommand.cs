using Maigret.Net.Cli.Rendering;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Maigret.Net.Cli.Commands;

/// <summary>
/// CLI entry point — port of <c>maigret/maigret.py main()</c>. Wires the
/// command-line settings into <see cref="MaigretSearchEngine.SearchAsync"/> and renders
/// results through <see cref="IResultRenderer"/>.
/// </summary>
public sealed class SearchCommand(
    Settings defaultSettings,
    MaigretDatabase embeddedDatabase,
    IActivationProvider activation,
    IIdExtractor extractor,
    IRecursiveSearchEngine recursive,
    IResultRenderer renderer,
    ReportPipeline reportPipeline,
    ILogger<SearchCommand> logger)
    : AsyncCommand<SearchCommandSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings cli, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cli);

        if (cli.NoColor)
        {
            AnsiConsole.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;
        }

        if (cli.Usernames.Length == 0)
        {
            renderer.RenderError("No usernames provided. Pass at least one positional argument.");
            return 2;
        }

        var settings = ApplyToSettings(defaultSettings, cli);
        var database = await ResolveDatabaseAsync(cli.Db, embeddedDatabase, cancellationToken).ConfigureAwait(false);

        var filter = new SearchFilter
        {
            IdType = cli.IdType,
            ScanAllSites = cli.AllSites,
            TopSites = cli.AllSites ? long.MaxValue : cli.TopSites,
            Tags = SplitCsv(cli.Tags),
            ExcludedTags = null,
            SiteNames = null,
        };

        // Move CLI-only knobs onto Settings where appropriate.
        if (!string.IsNullOrEmpty(cli.Ignore))
        {
            settings.IgnoreIdsList = SplitCsv(cli.Ignore)?.ToList() ?? settings.IgnoreIdsList;
        }

        renderer.RenderBanner(MaigretDefaults.Version);

        var usernames = ExpandUsernames(cli.Usernames, cli.Permute, cli.IdType);
        var checkers = MaigretSearchEngine.BuildDefaultCheckers(settings, logger);

        var allResults = new Dictionary<string, List<MaigretCheckResult>>(StringComparer.Ordinal);

        try
        {
            foreach (var username in usernames)
            {
                var sites = MaigretSearchEngine.SelectSites(database, settings, filter);
                renderer.RenderSearchStart(username, cli.IdType, sites.Count);

                var results = new List<MaigretCheckResult>();
                var recursionEnabled = !cli.NoRecursion && settings.RecursiveSearch;
                var recursiveOptions = new RecursiveSearchOptions
                {
                    MaxDepth = recursionEnabled ? 3 : 0,
                };

                var searchRequest = new MaigretSearchRequest
                {
                    Usernames = [username],
                    Database = database,
                    Settings = settings,
                    Filter = filter,
                    Activation = activation,
                    Extractor = extractor,
                    Logger = logger,
                    Checkers = checkers,
                };

                IAsyncEnumerable<MaigretCheckResult> stream = recursionEnabled
                    ? recursive.SearchAsync(searchRequest, recursiveOptions, cancellationToken)
                    : MaigretSearchEngine.SearchAsync(searchRequest, cancellationToken);

                await foreach (var r in stream.ConfigureAwait(false))
                {
                    results.Add(r);
                    var printAll = cli.PrintNotFound || cli.PrintErrors;
                    renderer.RenderResult(r, printAll);
                }

                var found = results.Count(r => r.Status == MaigretCheckStatus.Claimed);
                renderer.RenderSearchComplete(username, found);
                allResults[username] = results;
            }
        }
        finally
        {
            foreach (var c in checkers.Values)
            {
                try { await c.DisposeAsync().ConfigureAwait(false); }
                catch { /* best effort */ }
            }
        }

        if (cli.Txt || cli.Csv || cli.Json || cli.Markdown || cli.Html)
        {
            await PersistReports(allResults, cli, settings, cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }

    private async Task PersistReports(
        Dictionary<string, List<MaigretCheckResult>> allResults,
        SearchCommandSettings cli,
        Settings settings,
        CancellationToken ct)
    {
        var folder = string.IsNullOrEmpty(cli.FolderOutput) ? settings.ReportsPath : cli.FolderOutput;
        var generatedAt = DateTimeOffset.Now;
        var formats = new List<string>(capacity: 5);
        if (cli.Txt)
        {
            formats.Add("txt");
        }

        if (cli.Csv)
        {
            formats.Add("csv");
        }

        if (cli.Json)
        {
            formats.Add("json");
        }

        if (cli.Markdown)
        {
            formats.Add("markdown");
        }

        if (cli.Html)
        {
            formats.Add("html");
        }

        var unsupported = formats.Where(f => !reportPipeline.Supports(f)).ToList();
        if (unsupported.Count > 0)
        {
            renderer.RenderError(
                $"No registered IReportWriter for format(s) [{string.Join(", ", unsupported)}]. " +
                $"Available: [{string.Join(", ", reportPipeline.AvailableFormats)}].");
            formats.RemoveAll(unsupported.Contains);
        }

        if (formats.Count == 0)
        {
            return;
        }

        foreach (var (username, results) in allResults)
        {
            var ctx = new ReportContext(username, results, cli.IdType, generatedAt);
            await reportPipeline.WriteToFolderAsync(folder, ctx, formats, ct).ConfigureAwait(false);
        }
    }

    private static Settings ApplyToSettings(Settings template, SearchCommandSettings cli)
    {
        // Make a shallow copy so we don't mutate the DI-shared instance.
        var s = template;
        s.Timeout = cli.Timeout;
        s.RetriesCount = cli.Retries;
        s.MaxConnections = cli.MaxConnections;
        s.RecursiveSearch = !cli.NoRecursion;
        s.InfoExtracting = !cli.NoExtracting;
        if (!string.IsNullOrEmpty(cli.Proxy))
        {
            s.ProxyUrl = cli.Proxy;
        }

        if (!string.IsNullOrEmpty(cli.TorProxy))
        {
            s.TorProxyUrl = cli.TorProxy;
        }

        if (!string.IsNullOrEmpty(cli.I2pProxy))
        {
            s.I2pProxyUrl = cli.I2pProxy;
        }

        if (!string.IsNullOrEmpty(cli.CookiesJarFile))
        {
            s.CookieJarFile = cli.CookiesJarFile;
        }

        if (!string.IsNullOrEmpty(cli.FolderOutput))
        {
            s.ReportsPath = cli.FolderOutput;
        }

        s.TopSitesCount = cli.TopSites;
        s.ScanAllSites = cli.AllSites;
        s.PrintNotFound = cli.PrintNotFound;
        s.PrintCheckErrors = cli.PrintErrors;
        s.ColoredPrint = !cli.NoColor;
        s.TxtReport = cli.Txt;
        s.CsvReport = cli.Csv;
        s.JsonReportType = cli.Json ? "simple" : s.JsonReportType;
        s.HtmlReport = cli.Html;
        s.MdReport = cli.Markdown;
        s.DomainSearch = cli.WithDomains;
        return s;
    }

    private static async Task<MaigretDatabase> ResolveDatabaseAsync(string? source, MaigretDatabase fallback, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(source))
        {
            return fallback;
        }

        if (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            using var client = new HttpClient();
            return await new MaigretDatabase().LoadFromHttpAsync(source, client, ct).ConfigureAwait(false);
        }

        return new MaigretDatabase().LoadFromFile(source);
    }

    private static IReadOnlyCollection<string>? SplitCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return null;
        }

        var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? null : parts;
    }

    private static IReadOnlyList<string> ExpandUsernames(IReadOnlyList<string> usernames, bool permute, string idType)
    {
        // Mirror maigret.py: Permute is applied only with --permute, more than one
        // input, and the default id_type "username". Otherwise just return as-is
        // (with optional `{?}` wildcard expansion as a convenience).
        if (permute && usernames.Count > 1 && string.Equals(idType, "username", StringComparison.Ordinal))
        {
            var seed = usernames.ToDictionary(u => u, u => u, StringComparer.Ordinal);
            return [.. new Permute<string>(seed).Gather(PermuteMode.Strict).Keys];
        }

        var list = new List<string>(usernames.Count);
        foreach (var u in usernames)
        {
            list.Add(u);
            if (u.Contains("{?}", StringComparison.Ordinal))
            {
                list.Add(u.Replace("{?}", "_", StringComparison.Ordinal));
                list.Add(u.Replace("{?}", "-", StringComparison.Ordinal));
                list.Add(u.Replace("{?}", ".", StringComparison.Ordinal));
            }
        }
        return list;
    }
}
