namespace Maigret.Net.Core;

/// <summary>
/// Aggregate result returned by <see cref="MaigretClient.SearchAsync(string, MaigretSearchOptions?, System.Threading.CancellationToken)"/>.
/// Bundles the full result list with the convenience views consumers usually want.
/// </summary>
public sealed class MaigretSearchSummary(
    string username,
    IReadOnlyList<MaigretCheckResult> results,
    TimeSpan elapsed)
{

    /// <summary>The first username searched. For multi-username runs, see individual results.</summary>
    public string Username { get; } = username;

    /// <summary>All probed results, regardless of status.</summary>
    public IReadOnlyList<MaigretCheckResult> Results { get; } = results;

    /// <summary>Wall-clock duration of the search.</summary>
    public TimeSpan Elapsed { get; } = elapsed;

    /// <summary>Sites where the username was found.</summary>
    public IEnumerable<MaigretCheckResult> ClaimedSites =>
        Results.Where(r => r.Status == MaigretCheckStatus.Claimed);

    /// <summary>Sites where the username explicitly does not exist.</summary>
    public IEnumerable<MaigretCheckResult> AvailableSites =>
        Results.Where(r => r.Status == MaigretCheckStatus.Available);

    /// <summary>Sites that returned an error (bot protection, captcha, timeouts).</summary>
    public IEnumerable<MaigretCheckResult> Errors =>
        Results.Where(r => r.Status == MaigretCheckStatus.Unknown);

    /// <summary>Sites skipped because of disabled flag, wrong id type, or invalid username format.</summary>
    public IEnumerable<MaigretCheckResult> Skipped =>
        Results.Where(r => r.Status == MaigretCheckStatus.Illegal);

    /// <summary>Total sites probed.</summary>
    public int TotalChecked => Results.Count;

    /// <summary>Number of claimed accounts.</summary>
    public int FoundCount => ClaimedSites.Count();

    /// <summary>True when at least one site was claimed.</summary>
    public bool AnyFound => Results.Any(r => r.Status == MaigretCheckStatus.Claimed);

    /// <summary>
    /// Returns claimed sites grouped by username (handy when running with permutations
    /// or recursive search, where multiple usernames are probed in one call).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<MaigretCheckResult>> ClaimedByUsername =>
        Results.Where(r => r.Status == MaigretCheckStatus.Claimed)
               .GroupBy(r => r.Username, StringComparer.Ordinal)
               .ToDictionary(
                   g => g.Key,
                   g => (IReadOnlyList<MaigretCheckResult>)[.. g],
                   StringComparer.Ordinal);
}
