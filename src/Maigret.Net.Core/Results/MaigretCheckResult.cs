namespace Maigret.Net.Core.Results;

/// <summary>
/// Result of checking a username on a single site.
/// Mirrors <c>maigret.result.MaigretCheckResult</c>.
/// </summary>
public sealed class MaigretCheckResult(
    string username,
    string siteName,
    string siteUrlUser,
    MaigretCheckStatus status,
    IReadOnlyDictionary<string, string>? idsData = null,
    TimeSpan? queryTime = null,
    string? context = null,
    CheckError? error = null,
    IReadOnlyList<string>? tags = null)
{

    /// <summary>Username queried.</summary>
    public string Username { get; } = username;

    /// <summary>Site identifier (display name).</summary>
    public string SiteName { get; } = siteName;

    /// <summary>
    /// URL of the username's profile on the site. The site may or may not exist —
    /// this is only the URL the profile would have if it existed.
    /// </summary>
    public string SiteUrlUser { get; } = siteUrlUser;

    /// <inheritdoc cref="MaigretCheckStatus"/>
    public MaigretCheckStatus Status { get; } = status;

    /// <summary>
    /// Profile information extracted from the site response (e.g. internal IDs,
    /// linked usernames). Populated when extraction is enabled.
    /// </summary>
    public IReadOnlyDictionary<string, string>? IdsData { get; } = idsData;

    /// <summary>Time taken to perform the query.</summary>
    public TimeSpan? QueryTime { get; } = queryTime;

    /// <summary>Optional human-readable context for the result (e.g. error description).</summary>
    public string? Context { get; } = context;

    /// <summary>Error data when <see cref="Status"/> is <see cref="MaigretCheckStatus.Unknown"/>.</summary>
    public CheckError? Error { get; } = error;

    /// <summary>Site tags propagated to the result for downstream filtering.</summary>
    public IReadOnlyList<string> Tags { get; } = tags ?? [];

    /// <summary>True when the username was detected on the site.</summary>
    public bool IsFound => Status == MaigretCheckStatus.Claimed;

    public override string ToString()
    {
        var status = Status.ToString();
        return Context is null ? status : $"{status} ({Context})";
    }
}
