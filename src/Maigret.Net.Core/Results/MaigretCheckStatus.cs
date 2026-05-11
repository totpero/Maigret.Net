namespace Maigret.Net.Core.Results;

/// <summary>
/// Status of a username check on a given site.
/// Mirrors <c>maigret.result.MaigretCheckStatus</c>.
/// </summary>
public enum MaigretCheckStatus
{
    /// <summary>Username detected on the site.</summary>
    Claimed,

    /// <summary>Username not detected on the site.</summary>
    Available,

    /// <summary>Error occurred while trying to detect the username.</summary>
    Unknown,

    /// <summary>Username does not match the site's regex constraint.</summary>
    Illegal,
}
