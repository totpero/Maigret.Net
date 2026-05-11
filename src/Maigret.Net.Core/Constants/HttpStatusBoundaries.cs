namespace Maigret.Net.Core.Constants;

/// <summary>
/// HTTP status thresholds that drive Maigret's status interpretation.
/// </summary>
public static class HttpStatusBoundaries
{
    /// <summary>Lower bound (inclusive) of the 2xx success range.</summary>
    public const int OkLowerBound = 200;

    /// <summary>Upper bound (exclusive) of the 2xx success range.</summary>
    public const int OkUpperBound = 300;

    /// <summary>Lower bound (inclusive) of the 5xx server-error range.</summary>
    public const int ServerErrorLowerBound = 500;

    /// <summary>Forbidden — interpreted as "Access denied" unless ignored.</summary>
    public const int Forbidden = 403;

    /// <summary>LinkedIn-specific anti-bot code; treated as "not found".</summary>
    public const int LinkedInBlocked = 999;
}
