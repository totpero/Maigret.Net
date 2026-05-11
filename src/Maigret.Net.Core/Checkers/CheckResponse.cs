// Tuple returned by every <see cref="ICheckerBase"/> implementation.
namespace Maigret.Net.Core.Checkers;

/// <summary>
/// Result of a single HTTP/DNS probe. Mirrors the
/// <c>(html_text, status_code, error)</c> tuple returned by Python checkers.
/// </summary>
public readonly record struct CheckResponse(string Body, int StatusCode, CheckError? Error)
{
    /// <summary>Empty response with status code 0.</summary>
    public static readonly CheckResponse Empty = new(string.Empty, 0, null);
}
