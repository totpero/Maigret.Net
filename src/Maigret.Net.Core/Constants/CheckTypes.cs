namespace Maigret.Net.Core.Constants;

/// <summary>
/// Detection strategies a site declares via the <c>checkType</c> field.
/// </summary>
public static class CheckTypes
{
    /// <summary>2xx = claimed, otherwise available.</summary>
    public const string StatusCode = "status_code";

    /// <summary>Presence/absence substrings in the response body.</summary>
    public const string Message = "message";

    /// <summary>Successful redirect implies claimed (no 4xx/5xx).</summary>
    public const string ResponseUrl = "response_url";
}
