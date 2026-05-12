using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maigret.Net.Core;

/// <summary>
/// High-level synchronous-style entry point for the Maigret library.
/// <para>
/// Equivalent to invoking the CLI: instantiate the client, pass a username plus
/// a <see cref="MaigretSearchOptions"/> instance and you get a single
/// <see cref="MaigretSearchSummary"/> with all the matching sites.
/// </para>
/// <para>
/// For streaming consumption (results as they arrive), see
/// <see cref="StreamAsync(string, MaigretSearchOptions?, System.Threading.CancellationToken)"/>.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var client = new MaigretClient();
/// var summary = await client.SearchAsync("johndoe", new MaigretSearchOptions { TopSites = 50 });
/// foreach (var hit in summary.ClaimedSites)
/// {
///     Console.WriteLine($"{hit.SiteName}: {hit.SiteUrlUser}");
/// }
/// </code>
/// </example>
public sealed class MaigretClient(
    IActivationProvider? activation = null,
    IIdExtractor? extractor = null,
    IRecursiveSearchEngine? recursive = null,
    ILogger? logger = null)
{
    private static readonly Lazy<MaigretDatabase> EmbeddedDatabase =
        new(MaigretResources.LoadEmbeddedDatabase);

    private readonly IActivationProvider _activation = activation ?? NullActivationProvider.Instance;
    private readonly IIdExtractor _extractor = extractor ?? NullIdExtractor.Instance;
    private readonly IRecursiveSearchEngine _recursive = recursive ?? new RecursiveSearchEngine();
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    /// <summary>
    /// Searches the embedded site database for <paramref name="username"/> and
    /// returns the full result list once every probe has completed.
    /// </summary>
    public Task<MaigretSearchSummary> SearchAsync(
        string username,
        CancellationToken cancellationToken = default) =>
        SearchAsync(username, options: null, cancellationToken);

    /// <summary>
    /// Searches with the given <paramref name="options"/>. Equivalent to invoking
    /// the CLI with the same flags set.
    /// </summary>
    public async Task<MaigretSearchSummary> SearchAsync(
        string username,
        MaigretSearchOptions? options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var sw = Stopwatch.StartNew();
        var results = new List<MaigretCheckResult>();
        await foreach (var r in StreamAsync(username, options, cancellationToken).ConfigureAwait(false))
        {
            results.Add(r);
        }
        sw.Stop();

        return new MaigretSearchSummary(username, results, sw.Elapsed);
    }

    /// <summary>
    /// Streams individual <see cref="MaigretCheckResult"/> entries as each probe
    /// completes — preferred for long runs or live UI updates.
    /// </summary>
    public IAsyncEnumerable<MaigretCheckResult> StreamAsync(
        string username,
        MaigretSearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var opts = options ?? new MaigretSearchOptions();
        var usernames = BuildUsernameList(username, opts);
        var settings = BuildSettings(opts);
        var filter = BuildFilter(opts);

        var request = new MaigretSearchRequest
        {
            Usernames = usernames,
            Database = EmbeddedDatabase.Value,
            Settings = settings,
            Filter = filter,
            Activation = _activation,
            Extractor = opts.NoExtracting ? NullIdExtractor.Instance : _extractor,
            Notify = opts.Notify,
            Logger = _logger,
        };

        return opts.NoRecursion
            ? MaigretSearchEngine.SearchAsync(request, cancellationToken)
            : _recursive.SearchAsync(
                request,
                new RecursiveSearchOptions { MaxDepth = Math.Max(0, opts.RecursionDepth) },
                cancellationToken);
    }

    private static IReadOnlyList<string> BuildUsernameList(string primary, MaigretSearchOptions options)
    {
        var seed = new List<string> { primary };
        if (options.AdditionalUsernames is { Count: > 0 })
        {
            seed.AddRange(options.AdditionalUsernames);
        }

        if (!options.Permute || seed.Count <= 1 || !string.Equals(options.IdType, IdTypes.Username, StringComparison.Ordinal))
        {
            return seed;
        }

        var byKey = seed.ToDictionary(u => u, u => u, StringComparer.Ordinal);
        return [.. new Permute<string>(byKey).Gather(PermuteMode.Strict).Keys];
    }

    private static Settings BuildSettings(MaigretSearchOptions options)
    {
        var settings = Settings.LoadFromEmbedded();
        settings.Timeout = options.Timeout;
        settings.MaxConnections = options.MaxConnections;
        settings.RecursiveSearch = !options.NoRecursion;
        settings.InfoExtracting = !options.NoExtracting;
        settings.TopSitesCount = options.TopSites;
        settings.ScanAllSites = options.AllSites;
        settings.ScanDisabledSites = options.IncludeDisabled;

        if (!string.IsNullOrEmpty(options.Proxy))
        {
            settings.ProxyUrl = options.Proxy;
        }

        if (!string.IsNullOrEmpty(options.TorProxy))
        {
            settings.TorProxyUrl = options.TorProxy;
        }

        if (!string.IsNullOrEmpty(options.I2pProxy))
        {
            settings.I2pProxyUrl = options.I2pProxy;
        }

        if (!string.IsNullOrEmpty(options.CookiesJarFile))
        {
            settings.CookieJarFile = options.CookiesJarFile;
        }

        if (options.IgnoreSites is { Count: > 0 })
        {
            settings.IgnoreIdsList = [.. options.IgnoreSites];
        }

        return settings;
    }

    private static SearchFilter BuildFilter(MaigretSearchOptions options) => new()
    {
        IdType = options.IdType,
        ScanAllSites = options.AllSites,
        TopSites = options.AllSites ? long.MaxValue : options.TopSites,
        IncludeDisabled = options.IncludeDisabled,
        Tags = options.Tags,
        ExcludedTags = options.ExcludedTags,
        SiteNames = options.SiteNames,
    };
}
