namespace Maigret.Net.Core.Constants;

/// <summary>
/// HTTP header names used across activators and the checker pipeline.
/// </summary>
public static class HeaderNames
{
    public const string ContentType = "Content-Type";
    public const string Authorization = "Authorization";
    public const string Cookie = "cookie";
    public const string SetCookie = "Set-Cookie";
    public const string TwitterGuestToken = "x-guest-token";
    public const string OnlyFansSign = "sign";
    public const string OnlyFansTime = "time";
    public const string OnlyFansBypassCookie = "x-bc";
    public const string OnlyFansUserId = "user-id";
}
