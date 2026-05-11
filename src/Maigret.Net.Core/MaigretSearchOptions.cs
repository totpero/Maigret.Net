namespace Maigret.Net.Core;

/// <summary>
/// User-facing options for <see cref="MaigretClient.SearchAsync(string, MaigretSearchOptions?, System.Threading.CancellationToken)"/>.
/// One option per CLI flag — pass exactly the same shape you'd type on the command line.
/// </summary>
public sealed class MaigretSearchOptions
{
    /// <summary>Per-request HTTP timeout, in seconds. Default 30.</summary>
    public int Timeout { get; init; } = 30;

    /// <summary>Maximum concurrent HTTP requests. Default 100.</summary>
    public int MaxConnections { get; init; } = 100;

    /// <summary>Generic proxy URL (HTTP or SOCKS5). Optional.</summary>
    public string? Proxy { get; init; }

    /// <summary>Tor proxy URL (default <c>socks5://127.0.0.1:9050</c> when set).</summary>
    public string? TorProxy { get; init; }

    /// <summary>I2P proxy URL.</summary>
    public string? I2pProxy { get; init; }

    /// <summary>Mozilla-format cookies jar file path.</summary>
    public string? CookiesJarFile { get; init; }

    /// <summary>Disable recursive search by harvested IDs.</summary>
    public bool NoRecursion { get; init; }

    /// <summary>Disable profile-data extraction (no <see cref="MaigretCheckResult.IdsData"/> populated).</summary>
    public bool NoExtracting { get; init; }

    /// <summary>Identifier type to search by — <c>username</c>, <c>vk_id</c>, etc.</summary>
    public string IdType { get; init; } = IdTypes.Username;

    /// <summary>Permute multiple usernames with separators (<c>_</c>, <c>-</c>, <c>.</c>).</summary>
    public bool Permute { get; init; }

    /// <summary>Path or URL to a custom <c>data.json</c>. <c>null</c> uses the embedded database.</summary>
    public string? Db { get; init; }

    /// <summary>Probe domain names through DNS in addition to HTTP.</summary>
    public bool WithDomains { get; init; }

    /// <summary>Scan every site in the database (overrides <see cref="TopSites"/>).</summary>
    public bool AllSites { get; init; }

    /// <summary>Scan only the top N sites by Alexa rank. Default 500.</summary>
    public int TopSites { get; init; } = 500;

    /// <summary>Whitelist of tags to include (engine, category, country code).</summary>
    public IReadOnlyCollection<string>? Tags { get; init; }

    /// <summary>Tags to exclude.</summary>
    public IReadOnlyCollection<string>? ExcludedTags { get; init; }

    /// <summary>Whitelist of site names. Empty/null means "any".</summary>
    public IReadOnlyCollection<string>? SiteNames { get; init; }

    /// <summary>Site names to skip.</summary>
    public IReadOnlyCollection<string>? IgnoreSites { get; init; }

    /// <summary>Include sites flagged <c>disabled</c> in the database.</summary>
    public bool IncludeDisabled { get; init; }

    /// <summary>Maximum recursion depth for harvested IDs. Default 3.</summary>
    public int RecursionDepth { get; init; } = 3;

    /// <summary>Optional progress notifier.</summary>
    public QueryNotify? Notify { get; init; }

    /// <summary>Additional usernames to search (besides the primary one passed to SearchAsync).</summary>
    public IReadOnlyList<string>? AdditionalUsernames { get; init; }
}
