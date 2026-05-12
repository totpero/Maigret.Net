// Port of maigret/types.py — small typed structures shared across the engine.
// In Python these are loose Dict[str, Any]; in .NET we model them strongly.

using System.Net;

namespace Maigret.Net.Core;

/// <summary>
/// Per-search options passed to the checking engine. Mirrors the keys of
/// <c>maigret.types.QueryOptions</c> with concrete types.
/// </summary>
public sealed class QueryOptions
{
    /// <summary>Per-request HTTP timeout. <c>null</c> means no timeout.</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>Whether to extract IDs from successful responses.</summary>
    public bool Parsing { get; set; } = true;

    /// <summary>If true, sites marked <c>disabled</c> are still checked.</summary>
    public bool Forced { get; set; }

    /// <summary>Identifier type to filter sites by (default <c>username</c>).</summary>
    public string IdType { get; set; } = "username";

    /// <summary>Optional cookie container shared across requests.</summary>
    public CookieContainer? CookieJar { get; set; }

    /// <summary>
    /// Map of site protocol → checker instance. The default protocol key is
    /// <see cref="DefaultCheckerKey"/>; sites with custom <c>protocol</c> values
    /// should have a matching entry.
    /// </summary>
    public Dictionary<string, ICheckerBase> Checkers { get; set; } = [];

    /// <summary>Conventional protocol key used when a site declares no protocol.</summary>
    public const string DefaultCheckerKey = "";
}
