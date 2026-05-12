// Top-level orchestrator. Mirrors `async def maigret(...)` from maigret/maigret.py:
// filters sites, fans out per-(site, username) checks, and streams results back as
// they complete. Channel<T> + SemaphoreSlim replace asyncio.Queue + Semaphore.

using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maigret.Net.Core.Search;

/// <summary>
/// Orchestrates a multi-username, multi-site search. Mirrors the top-level
/// <c>async def maigret(...)</c> from <c>maigret/maigret.py</c>.
/// </summary>
public static class MaigretSearchEngine
{
    /// <summary>
    /// Streams <see cref="MaigretCheckResult"/> entries as each (site, username) probe
    /// completes. Use <c>await foreach</c> on the result.
    /// </summary>
    public static async IAsyncEnumerable<MaigretCheckResult> SearchAsync(
        MaigretSearchRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Usernames);
        ArgumentNullException.ThrowIfNull(request.Database);
        ArgumentNullException.ThrowIfNull(request.Settings);

        var context = ResolveContext(request);
        if (context is null)
        {
            yield break;
        }

        var channel = CreateChannel(context.Sites.Count * context.Usernames.Count);
        using var semaphore = new SemaphoreSlim(
            Math.Max(1, context.Settings.MaxConnections),
            Math.Max(1, context.Settings.MaxConnections));

        var workers = ScheduleWorkers(context, channel.Writer, semaphore, cancellationToken);
        var pump = StartCompletionPump(workers, channel.Writer, cancellationToken);

        try
        {
            await foreach (var r in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return r;
            }
        }
        finally
        {
            await DrainPumpAsync(pump).ConfigureAwait(false);
            context.Notify?.Finish();
            if (context.OwnsCheckers)
            {
                await DisposeCheckersAsync(context.Checkers, context.Logger).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Filters the database according to <paramref name="settings"/> and
    /// <paramref name="filter"/> and returns the resulting site list ordered by Alexa rank.
    /// </summary>
    public static IReadOnlyList<MaigretSite> SelectSites(
        MaigretDatabase database,
        Settings settings,
        SearchFilter filter)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(filter);

        var top = ResolveTopCap(settings, filter);
        var names = filter.SiteNames is { Count: > 0 } ? filter.SiteNames : settings.ScanSitesList;
        var includeDisabled = filter.IncludeDisabled || settings.ScanDisabledSites;

        var ranked = database.RankedSitesDict(
            top: top,
            tags: filter.Tags,
            excludedTags: filter.ExcludedTags,
            names: names,
            includeDisabled: includeDisabled,
            idType: filter.IdType);

        if (settings.IgnoreIdsList is not { Count: > 0 })
        {
            return [.. ranked.Values];
        }

        var ignored = new HashSet<string>(settings.IgnoreIdsList, StringComparer.OrdinalIgnoreCase);
        return [.. ranked.Values.Where(s => !ignored.Contains(s.Name))];
    }

    /// <summary>
    /// Builds the default checker dictionary used when callers don't supply one.
    /// Returns a single shared <see cref="SimpleHttpChecker"/> (or <see cref="ProxiedHttpChecker"/>
    /// when <see cref="Settings.ProxyUrl"/> is set) registered under the default key.
    /// </summary>
    public static IDictionary<string, ICheckerBase> BuildDefaultCheckers(Settings settings, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var cookies = !string.IsNullOrEmpty(settings.CookieJarFile)
            ? new CookieContainer()
            : null;

        ICheckerBase checker = !string.IsNullOrEmpty(settings.ProxyUrl)
            ? new ProxiedHttpChecker(settings.ProxyUrl!, logger, cookies)
            : new SimpleHttpChecker(logger, proxy: null, cookies);

        return new Dictionary<string, ICheckerBase> { [QueryOptions.DefaultCheckerKey] = checker };
    }

    // ---------------------------------------------------------------------
    // Private helpers — extracted to keep SearchAsync readable.
    // ---------------------------------------------------------------------

    private static ResolvedSearchContext? ResolveContext(MaigretSearchRequest request)
    {
        var settings = request.Settings;
        var filter = request.Filter ?? new SearchFilter();
        var logger = request.Logger ?? NullLogger.Instance;

        var usernames = request.Usernames.Where(u => !string.IsNullOrWhiteSpace(u)).ToArray();
        if (usernames.Length == 0)
        {
            return null;
        }

        var sites = SelectSites(request.Database, settings, filter);
        if (sites.Count == 0)
        {
            return null;
        }

        var ownsCheckers = request.Checkers is null;
        var checkers = request.Checkers ?? BuildDefaultCheckers(settings, logger);

        return new ResolvedSearchContext
        {
            Usernames = usernames,
            Sites = sites,
            Checkers = checkers,
            OwnsCheckers = ownsCheckers,
            Options = BuildOptions(settings, filter, checkers),
            Filter = filter,
            Activation = request.Activation ?? NullActivationProvider.Instance,
            Extractor = request.Extractor ?? NullIdExtractor.Instance,
            Logger = logger,
            Settings = settings,
            Notify = request.Notify,
        };
    }

    private static QueryOptions BuildOptions(Settings settings, SearchFilter filter, IDictionary<string, ICheckerBase> checkers) => new()
    {
        Timeout = TimeSpan.FromSeconds(Math.Max(1, settings.Timeout)),
        Parsing = settings.InfoExtracting,
        Forced = settings.ScanDisabledSites || filter.IncludeDisabled,
        IdType = filter.IdType,
        Checkers = new Dictionary<string, ICheckerBase>(checkers),
    };

    private static long ResolveTopCap(Settings settings, SearchFilter filter)
    {
        if (filter.ScanAllSites || settings.ScanAllSites)
        {
            return long.MaxValue;
        }

        return filter.TopSites is { } explicitCap && explicitCap > 0 ? explicitCap : Math.Max(1, settings.TopSitesCount);
    }

    private static Channel<MaigretCheckResult> CreateChannel(int capacity) =>
        Channel.CreateBounded<MaigretCheckResult>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        });

    private static List<Task> ScheduleWorkers(
        ResolvedSearchContext context,
        ChannelWriter<MaigretCheckResult> writer,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        var workers = new List<Task>(capacity: context.Sites.Count * context.Usernames.Count);
        foreach (var username in context.Usernames)
        {
            context.Notify?.Start(username, context.Filter.IdType);
            foreach (var site in context.Sites)
            {
                workers.Add(Task.Run(
                    () => ProbeSiteAsync(context, site, username, writer, semaphore, cancellationToken),
                    cancellationToken));
            }
        }
        return workers;
    }

    private static async Task ProbeSiteAsync(
        ResolvedSearchContext context,
        MaigretSite site,
        string username,
        ChannelWriter<MaigretCheckResult> writer,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await Checking.CheckSiteForUsernameAsync(
                    site, username, context.Options,
                    context.Notify, context.Activation, context.Extractor,
                    context.Logger, cancellationToken)
                .ConfigureAwait(false);
            await writer.WriteAsync(result, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
        catch (Exception ex)
        {
            context.Logger.LogDebug(ex, "Worker error for {Site}/{User}", site.Name, username);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static Task StartCompletionPump(
        List<Task> workers,
        ChannelWriter<MaigretCheckResult> writer,
        CancellationToken cancellationToken) =>
        Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(workers).ConfigureAwait(false);
            }
            finally
            {
                writer.TryComplete();
            }
        }, cancellationToken);

    private static async Task DrainPumpAsync(Task pump)
    {
        try
        {
            await pump.ConfigureAwait(false);
        }
        catch
        {
            // Worker errors surface through individual results; suppress secondary errors here.
        }
    }

    private static async Task DisposeCheckersAsync(IDictionary<string, ICheckerBase> checkers, ILogger logger)
    {
        foreach (var checker in checkers.Values)
        {
            try
            {
                await checker.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Checker disposal raised; ignoring");
            }
        }
    }
}
