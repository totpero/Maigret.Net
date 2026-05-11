namespace Maigret.Net.Core.Search;

/// <summary>
/// Filtering options consumed by <see cref="MaigretSearchEngine.SearchAsync"/>. These are
/// the runtime knobs Maigret takes via CLI flags; they are kept separate from
/// <see cref="Settings"/> (which mirrors the persistable JSON config).
/// </summary>
public sealed class SearchFilter
{
    /// <summary>Tags the site must satisfy (engine, tag, protocol).</summary>
    public IReadOnlyCollection<string>? Tags { get; init; }

    /// <summary>Tags that disqualify the site.</summary>
    public IReadOnlyCollection<string>? ExcludedTags { get; init; }

    /// <summary>Whitelist of site names. Empty/null means "any".</summary>
    public IReadOnlyCollection<string>? SiteNames { get; init; }

    /// <summary>Identifier type sites must declare (default <c>username</c>).</summary>
    public string IdType { get; init; } = IdTypes.Username;

    /// <summary>Cap on the number of sites scanned. <c>0</c> / <c>null</c> means "use defaults".</summary>
    public long? TopSites { get; init; }

    /// <summary>If true, sites flagged <c>disabled</c> are still scanned.</summary>
    public bool IncludeDisabled { get; init; }

    /// <summary>If true, ignore <see cref="TopSites"/> and scan everything.</summary>
    public bool ScanAllSites { get; init; }
}
