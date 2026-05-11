// Port of maigret/checking.py — orchestrates a single (site, username) check.
using System.Diagnostics;
using System.Text.RegularExpressions;
using Maigret.Net.Core.Checkers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maigret.Net.Core.Search;

/// <summary>
/// Pipeline that probes a site for a username and turns the response into a
/// <see cref="MaigretCheckResult"/>. Mirrors the procedural flow of
/// <c>checking.check_site_for_username</c>, <c>make_site_result</c>, and
/// <c>process_site_result</c>.
/// </summary>
public static class Checking
{
    /// <summary>Identifier types Maigret can search by. See <see cref="IdTypes"/>.</summary>
    public static readonly IReadOnlySet<string> SupportedIds = new HashSet<string>(StringComparer.Ordinal)
    {
        IdTypes.Username,
        IdTypes.YandexPublicId,
        IdTypes.GaiaId,
        IdTypes.VkId,
        IdTypes.OkId,
        IdTypes.WikimapiaUid,
        IdTypes.SteamId,
        IdTypes.UidmeUguid,
        IdTypes.YelpUserId,
    };

    /// <summary>Characters that disqualify a username outright.</summary>
    public const string BadChars = "#";

    private static readonly Regex MultiSlashRegex = new(@"(?<!:)/+", RegexOptions.Compiled);

    /// <summary>
    /// Returns the <see cref="CheckError"/> implied by a response body / status code,
    /// or <c>null</c> when no error is detected.
    /// Mirrors <c>checking.detect_error_page</c>.
    /// </summary>
    public static CheckError? DetectErrorPage(
        string htmlText,
        int statusCode,
        IReadOnlyDictionary<string, string>? failFlags,
        bool ignore403)
    {
        if (failFlags is not null)
        {
            foreach (var (flag, msg) in failFlags)
            {
                if (htmlText.Contains(flag, StringComparison.Ordinal))
                {
                    return new CheckError("Site-specific", msg);
                }
            }
        }

        var common = CommonErrors.Detect(htmlText);
        if (common is not null)
        {
            return common;
        }

        if (statusCode == HttpStatusBoundaries.Forbidden && !ignore403)
        {
            return new CheckError("Access denied", "403 status code, use proxy/vpn");
        }

        if (statusCode == HttpStatusBoundaries.LinkedInBlocked)
        {
            // LinkedIn anti-bot — treat as not found, not infrastructure error.
            return null;
        }

        if (statusCode >= HttpStatusBoundaries.ServerErrorLowerBound)
        {
            return new CheckError("Server", $"{statusCode} status code");
        }

        return null;
    }

    /// <summary>
    /// Performs status-only interpretation of a probe response, *without* running
    /// activation or ID extraction. Useful in unit tests.
    /// Mirrors the status-resolution branch of <c>process_site_result</c>.
    /// </summary>
    public static MaigretCheckResult InterpretResponse(
        MaigretSite site,
        string username,
        string profileUrl,
        CheckResponse response)
    {
        var fullTags = site.Tags.ToArray();

        if (response.Error is not null)
        {
            return new MaigretCheckResult(
                username, site.PrettyName, profileUrl, MaigretCheckStatus.Unknown,
                error: response.Error,
                context: response.Error.ToString(),
                tags: fullTags);
        }

        var combinedErrors = CombineErrors(site);
        var detected = DetectErrorPage(response.Body, response.StatusCode, combinedErrors, site.Ignore403);
        if (detected is not null)
        {
            return new MaigretCheckResult(
                username, site.PrettyName, profileUrl, MaigretCheckStatus.Unknown,
                error: detected,
                context: detected.ToString(),
                tags: fullTags);
        }

        var (presenseDetected, _) = DetectPresence(site, response.Body);
        var status = ResolveStatus(site, response, presenseDetected);

        return new MaigretCheckResult(username, site.PrettyName, profileUrl, status, tags: fullTags);
    }

    /// <summary>
    /// Full per-site pipeline: prepares the request, runs the probe, optionally
    /// re-runs it after activation, interprets the response, and (if enabled)
    /// extracts IDs. Mirrors <c>checking.check_site_for_username</c>.
    /// </summary>
    public static async Task<MaigretCheckResult> CheckSiteForUsernameAsync(
        MaigretSite site,
        string username,
        QueryOptions options,
        QueryNotify? notify = null,
        IActivationProvider? activation = null,
        IIdExtractor? extractor = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(site);
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(options);

        logger ??= NullLogger.Instance;
        activation ??= NullActivationProvider.Instance;
        extractor ??= NullIdExtractor.Instance;

        var fullTags = site.Tags.ToArray();
        var profileUrl = BuildProfileUrl(site, username);

        var pre = ValidatePrecondition(site, username, profileUrl, options, fullTags, logger);
        if (pre is not null)
        {
            return pre;
        }

        var checker = ResolveChecker(site, options);
        if (checker is null)
        {
            return BuildIllegal(username, site, profileUrl, fullTags, MaigretCheckStatus.Unknown,
                new CheckError("Unexpected", $"No checker for site {site.Name}"));
        }

        var (response, elapsed) = await RunProbeAsync(
                site, username, profileUrl, options, checker, activation, logger, cancellationToken)
            .ConfigureAwait(false);

        var interpretation = InterpretResponse(site, username, profileUrl, response);
        var ids = TryExtractIds(extractor, response, site, options, interpretation.Status, logger);

        var enriched = WithEnrichedFields(interpretation, ids, elapsed);
        notify?.Update(enriched, isSimilar: site.SimilarSearch);
        return enriched;
    }

    /// <summary>
    /// Quick illegality / bad-input checks; returns the appropriate
    /// <see cref="MaigretCheckResult"/> when the username/site combo can't be probed,
    /// or <c>null</c> when the pipeline should proceed.
    /// </summary>
    private static MaigretCheckResult? ValidatePrecondition(
        MaigretSite site,
        string username,
        string profileUrl,
        QueryOptions options,
        IReadOnlyList<string> fullTags,
        ILogger logger)
    {
        if (site.Disabled && !options.Forced)
        {
            return BuildIllegal(username, site, profileUrl, fullTags, MaigretCheckStatus.Illegal,
                new CheckError("Check is disabled"));
        }

        if (!string.Equals(site.Type, options.IdType, StringComparison.Ordinal))
        {
            return BuildIllegal(username, site, profileUrl, fullTags, MaigretCheckStatus.Illegal,
                new CheckError("Unsupported identifier type", $"Want \"{site.Type}\""));
        }

        var regexFailure = ValidateUsernameRegex(site, username, logger);
        if (regexFailure is not null)
        {
            return BuildIllegal(username, site, profileUrl, fullTags, MaigretCheckStatus.Illegal, regexFailure);
        }

        foreach (var bad in BadChars)
        {
            if (username.Contains(bad, StringComparison.Ordinal))
            {
                return BuildIllegal(username, site, profileUrl, fullTags, MaigretCheckStatus.Illegal,
                    new CheckError("Unsupported username format", $"Contains '{bad}'"));
            }
        }

        return null;
    }

    private static CheckError? ValidateUsernameRegex(MaigretSite site, string username, ILogger logger)
    {
        if (string.IsNullOrEmpty(site.RegexCheck))
        {
            return null;
        }

        try
        {
            if (!Regex.IsMatch(username, site.RegexCheck!))
            {
                return new CheckError("Unsupported username format", $"Want \"{site.RegexCheck}\"");
            }
        }
        catch (RegexParseException ex)
        {
            logger.LogDebug(ex, "Invalid regexCheck for site {Site}", site.Name);
        }
        return null;
    }

    private static MaigretCheckResult BuildIllegal(
        string username,
        MaigretSite site,
        string profileUrl,
        IReadOnlyList<string> fullTags,
        MaigretCheckStatus status,
        CheckError error) =>
        new(username, site.PrettyName, profileUrl, status, error: error, tags: fullTags);

    private static async Task<(CheckResponse Response, TimeSpan Elapsed)> RunProbeAsync(
        MaigretSite site,
        string username,
        string profileUrl,
        QueryOptions options,
        ICheckerBase checker,
        IActivationProvider activation,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var probeUrl = BuildProbeUrl(site, username, profileUrl);
        var headers = BuildHeaders(site);
        var method = ResolveMethod(site);
        var allowRedirects = !string.Equals(site.CheckType, CheckTypes.ResponseUrl, StringComparison.Ordinal);
        var payload = BuildPayload(site, username);

        var stopwatch = Stopwatch.StartNew();
        var response = await checker
            .CheckAsync(probeUrl, headers, allowRedirects, options.Timeout, method, payload, cancellationToken)
            .ConfigureAwait(false);

        if (activation.CanActivate(site) && ResponseNeedsActivation(site, response.Body))
        {
            response = await TryReprobeWithActivationAsync(
                    site, probeUrl, options, checker, activation, response,
                    allowRedirects, method, payload, logger, cancellationToken)
                .ConfigureAwait(false);
        }

        stopwatch.Stop();
        return (response, stopwatch.Elapsed);
    }

    private static async Task<CheckResponse> TryReprobeWithActivationAsync(
        MaigretSite site,
        string probeUrl,
        QueryOptions options,
        ICheckerBase checker,
        IActivationProvider activation,
        CheckResponse current,
        bool allowRedirects,
        string method,
        object? payload,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await activation.ActivateAsync(site, probeUrl, cancellationToken).ConfigureAwait(false);
            var refreshedHeaders = BuildHeaders(site);
            return await checker
                .CheckAsync(probeUrl, refreshedHeaders, allowRedirects, options.Timeout, method, payload, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Activation failed for {Site}", site.Name);
            return current;
        }
    }

    private static IReadOnlyDictionary<string, string>? TryExtractIds(
        IIdExtractor extractor,
        CheckResponse response,
        MaigretSite site,
        QueryOptions options,
        MaigretCheckStatus status,
        ILogger logger)
    {
        if (!options.Parsing || status != MaigretCheckStatus.Claimed)
        {
            return null;
        }

        try
        {
            var extracted = extractor.Extract(response.Body, site);
            return extracted.Count > 0 ? extracted : null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "ID extraction failed for {Site}", site.Name);
            return null;
        }
    }

    private static MaigretCheckResult WithEnrichedFields(
        MaigretCheckResult source,
        IReadOnlyDictionary<string, string>? ids,
        TimeSpan elapsed) =>
        new(source.Username, source.SiteName, source.SiteUrlUser, source.Status,
            idsData: ids,
            queryTime: elapsed,
            context: source.Context,
            error: source.Error,
            tags: source.Tags);

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Resolves a site's URL template into a fully-qualified profile URL for the username.
    /// </summary>
    public static string BuildProfileUrl(MaigretSite site, string username)
    {
        if (string.IsNullOrEmpty(site.Url))
        {
            return site.UrlMain;
        }

        var encoded = Uri.EscapeDataString(username);
        var url = site.Url
            .Replace("{urlMain}", site.UrlMain, StringComparison.Ordinal)
            .Replace("{urlSubpath}", site.UrlSubpath, StringComparison.Ordinal)
            .Replace("{username}", encoded, StringComparison.Ordinal);

        return MultiSlashRegex.Replace(url, "/");
    }

    private static string BuildProbeUrl(MaigretSite site, string username, string defaultUrl)
    {
        if (string.IsNullOrEmpty(site.UrlProbe))
        {
            return defaultUrl;
        }

        var probe = site.UrlProbe!
            .Replace("{urlMain}", site.UrlMain, StringComparison.Ordinal)
            .Replace("{urlSubpath}", site.UrlSubpath, StringComparison.Ordinal)
            .Replace("{username}", username, StringComparison.Ordinal);

        return MultiSlashRegex.Replace(probe, "/");
    }

    private static IReadOnlyDictionary<string, string> BuildHeaders(MaigretSite site)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["User-Agent"] = MaigretUtilities.GetRandomUserAgent(),
            ["Connection"] = "close",
        };

        // Engine-level headers, if any, are merged into Site.Headers via UpdateFromEngine.
        foreach (var (k, v) in site.Headers)
        {
            headers[k] = v;
        }

        return headers;
    }

    private static string ResolveMethod(MaigretSite site)
    {
        if (!string.IsNullOrEmpty(site.RequestMethod))
        {
            return site.RequestMethod.ToLowerInvariant();
        }

        if (string.Equals(site.CheckType, CheckTypes.StatusCode, StringComparison.Ordinal) &&
            !string.IsNullOrEmpty(site.RequestHeadOnly) &&
            !string.Equals(site.RequestHeadOnly, "false", StringComparison.OrdinalIgnoreCase))
        {
            return "head";
        }

        return "get";
    }

    private static object? BuildPayload(MaigretSite site, string username)
    {
        if (site.RequestPayload.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return null;
        }

        var dict = new Dictionary<string, object?>();
        foreach (var prop in site.RequestPayload.EnumerateObject())
        {
            dict[prop.Name] = prop.Value.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String =>
                    (prop.Value.GetString() ?? string.Empty).Replace("{username}", username, StringComparison.Ordinal),
                _ => prop.Value.ToString(),
            };
        }

        return dict;
    }

    private static ICheckerBase? ResolveChecker(MaigretSite site, QueryOptions options)
    {
        var key = site.Protocol ?? string.Empty;
        if (options.Checkers.TryGetValue(key, out var checker))
        {
            return checker;
        }

        if (options.Checkers.TryGetValue(QueryOptions.DefaultCheckerKey, out var fallback))
        {
            return fallback;
        }

        return options.Checkers.Values.FirstOrDefault();
    }

    private static IReadOnlyDictionary<string, string>? CombineErrors(MaigretSite site)
    {
        if (site.Errors.Count == 0 && site.EngineObj is null)
        {
            return null;
        }

        var combined = new Dictionary<string, string>(StringComparer.Ordinal);
        if (site.EngineObj?.Site.ValueKind == System.Text.Json.JsonValueKind.Object &&
            site.EngineObj.Site.TryGetProperty("errors", out var engineErrors) &&
            engineErrors.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            foreach (var prop in engineErrors.EnumerateObject())
            {
                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    combined[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }
        }
        foreach (var (k, v) in site.Errors)
        {
            combined[k] = v;
        }

        return combined;
    }

    private static (bool Detected, string? Flag) DetectPresence(MaigretSite site, string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return (false, null);
        }

        if (site.PresenseStrs.Count == 0)
        {
            return (true, null);
        }

        foreach (var flag in site.PresenseStrs)
        {
            if (body.Contains(flag, StringComparison.Ordinal))
            {
                return (true, flag);
            }
        }
        return (false, null);
    }

    private static MaigretCheckStatus ResolveStatus(MaigretSite site, CheckResponse response, bool presenseDetected)
    {
        return site.CheckType switch
        {
            CheckTypes.Message => ResolveMessageStatus(site, response, presenseDetected),
            CheckTypes.StatusCode => IsSuccess(response.StatusCode)
                ? MaigretCheckStatus.Claimed
                : MaigretCheckStatus.Available,
            CheckTypes.ResponseUrl => IsSuccess(response.StatusCode) && presenseDetected
                ? MaigretCheckStatus.Claimed
                : MaigretCheckStatus.Available,
            _ => throw new InvalidOperationException($"Unknown check type '{site.CheckType}' for site '{site.Name}'"),
        };
    }

    private static bool IsSuccess(int statusCode) =>
        statusCode >= HttpStatusBoundaries.OkLowerBound &&
        statusCode < HttpStatusBoundaries.OkUpperBound;

    private static MaigretCheckStatus ResolveMessageStatus(MaigretSite site, CheckResponse response, bool presenseDetected)
    {
        if (string.IsNullOrEmpty(response.Body))
        {
            return presenseDetected ? MaigretCheckStatus.Claimed : MaigretCheckStatus.Available;
        }

        foreach (var flag in site.AbsenceStrs)
        {
            if (response.Body.Contains(flag, StringComparison.Ordinal))
            {
                return MaigretCheckStatus.Available;
            }
        }

        return presenseDetected ? MaigretCheckStatus.Claimed : MaigretCheckStatus.Available;
    }

    private static bool ResponseNeedsActivation(MaigretSite site, string body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return false;
        }

        if (site.Activation.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return false;
        }

        if (!site.Activation.TryGetProperty(ActivationKeys.Marks, out var marks) ||
            marks.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return false;
        }

        foreach (var mark in marks.EnumerateArray())
        {
            if (mark.ValueKind == System.Text.Json.JsonValueKind.String &&
                body.Contains(mark.GetString() ?? string.Empty, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }
}
