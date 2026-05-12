using System.Text.RegularExpressions;

namespace Maigret.Net.Core.Utils;

/// <summary>
/// Helpers for matching profile URLs and extracting usernames from them.
/// Mirrors <c>maigret.utils.URLMatcher</c>.
/// </summary>
public static class UrlMatcher
{
    private const string HttpUrlPattern = @"^https?://(www\.|m\.)?(.+)$";
    private const string UnsafeSymbols = ".?";

    private static readonly Regex HttpUrlRegex = new(HttpUrlPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Returns the host + path portion of a URL with the scheme and the
    /// <c>www.</c>/<c>m.</c> prefix stripped, mirroring
    /// <c>URLMatcher.extract_main_part</c>.
    /// </summary>
    public static string ExtractMainPart(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        var match = HttpUrlRegex.Match(url);
        return match.Success && match.Groups[2].Value.Length > 0 ? match.Groups[2].Value.TrimEnd('/') : string.Empty;
    }

    /// <summary>
    /// Builds a regex that matches a profile URL of the given pattern, capturing the
    /// username at the <c>{username}</c> placeholder.
    /// </summary>
    public static Regex? MakeProfileUrlRegexp(string url, string usernameRegex)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        var main = ExtractMainPart(url);
        if (string.IsNullOrEmpty(main))
        {
            return null;
        }

        foreach (var c in UnsafeSymbols)
        {
            main = main.Replace(c.ToString(), "\\" + c, StringComparison.Ordinal);
        }

        var preparedUsername = string.IsNullOrEmpty(usernameRegex)
            ? ".+?"
            : usernameRegex.TrimStart('^').TrimEnd('$');

        var urlRegex = main.Replace("{username}", $"({preparedUsername})", StringComparison.Ordinal);
        var pattern = HttpUrlPattern.Replace("(.+)", urlRegex);

        try
        {
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
