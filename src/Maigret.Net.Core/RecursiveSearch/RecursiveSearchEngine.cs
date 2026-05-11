// Default IRecursiveSearchEngine. Maintains a depth-limited queue of (id, type)
// pairs, drives MaigretSearchEngine.SearchAsync per id-type group, and harvests new ids
// from claimed results via the same parse rules as Python's parse_usernames.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maigret.Net.Core.RecursiveSearch;

/// <summary>
/// Default recursive search orchestrator. Mirrors the Python control flow:
/// run a search → extract IDs → enqueue new ones → repeat until exhausted or
/// <see cref="RecursiveSearchOptions.MaxDepth"/> is hit.
/// </summary>
public sealed class RecursiveSearchEngine : IRecursiveSearchEngine
{
    public async IAsyncEnumerable<MaigretCheckResult> SearchAsync(
        MaigretSearchRequest request,
        RecursiveSearchOptions? recursive = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Usernames);
        ArgumentNullException.ThrowIfNull(request.Database);
        ArgumentNullException.ThrowIfNull(request.Settings);

        var logger = request.Logger ?? NullLogger.Instance;
        var options = recursive ?? new RecursiveSearchOptions();
        var rootIdType = request.Filter?.IdType ?? "username";

        var seen = new HashSet<(string Id, string Type)>(IdComparer.Instance);
        var queue = new Queue<QueueItem>();

        var seedBatch = request.Usernames
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => (Id: u, Type: rootIdType))
            .Where(x => seen.Add(x))
            .ToList();

        if (seedBatch.Count == 0)
        {
            yield break;
        }

        queue.Enqueue(new QueueItem(0, seedBatch));

        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = queue.Dequeue();

            foreach (var group in current.Batch.GroupBy(x => x.Type, StringComparer.Ordinal))
            {
                var ids = group.Select(x => x.Id).ToList();
                var groupRequest = WithIdsAndType(request, ids, group.Key);

                await foreach (var result in MaigretSearchEngine
                                   .SearchAsync(groupRequest, cancellationToken)
                                   .ConfigureAwait(false))
                {
                    yield return result;

                    if (options.MaxDepth <= 0 ||
                        current.Depth + 1 >= options.MaxDepth ||
                        result.Status != MaigretCheckStatus.Claimed ||
                        result.IdsData is null ||
                        result.IdsData.Count == 0)
                    {
                        continue;
                    }

                    var newIds = ParseUsernames(result.IdsData)
                        .Where(x => seen.Add(x))
                        .Take(Math.Max(1, options.MaxIdsPerResult))
                        .ToList();

                    if (newIds.Count > 0)
                    {
                        logger.LogDebug(
                            "Recursion: depth {Depth} + {Count} new ids from {Site}",
                            current.Depth + 1, newIds.Count, result.SiteName);
                        queue.Enqueue(new QueueItem(current.Depth + 1, newIds));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns a copy of <paramref name="source"/> with its <see cref="MaigretSearchRequest.Usernames"/>
    /// and <see cref="SearchFilter.IdType"/> swapped out.
    /// </summary>
    private static MaigretSearchRequest WithIdsAndType(MaigretSearchRequest source, IReadOnlyList<string> ids, string idType)
    {
        var sourceFilter = source.Filter;
        var groupFilter = sourceFilter is null
            ? new SearchFilter { IdType = idType }
            : new SearchFilter
            {
                IdType = idType,
                Tags = sourceFilter.Tags,
                ExcludedTags = sourceFilter.ExcludedTags,
                SiteNames = sourceFilter.SiteNames,
                TopSites = sourceFilter.TopSites,
                IncludeDisabled = sourceFilter.IncludeDisabled,
                ScanAllSites = sourceFilter.ScanAllSites,
            };

        return new MaigretSearchRequest
        {
            Usernames = ids,
            Database = source.Database,
            Settings = source.Settings,
            Filter = groupFilter,
            Activation = source.Activation,
            Extractor = source.Extractor,
            Notify = source.Notify,
            Logger = source.Logger,
            Checkers = source.Checkers,
        };
    }

    /// <summary>
    /// Returns the candidate <c>(id, type)</c> pairs implied by an extracted
    /// dictionary. Mirrors <c>checking.parse_usernames</c>.
    /// </summary>
    public static IEnumerable<(string Id, string Type)> ParseUsernames(IReadOnlyDictionary<string, string> idsData)
    {
        if (idsData is null)
        {
            yield break;
        }

        foreach (var (rawKey, rawValue) in idsData)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }
            var key = rawKey.ToLowerInvariant();
            var value = rawValue.Trim();

            if ((key.Contains("username") && !key.Contains("usernames")) ||
                key == "screen_name" ||
                key == "login")
            {
                yield return (value, "username");
            }

            if (key.Contains("usernames"))
            {
                foreach (var u in SplitListLiteral(value))
                {
                    yield return (u, "username");
                }
            }

            if (Checking.SupportedIds.Contains(rawKey, StringComparer.Ordinal))
            {
                yield return (value, rawKey);
            }
        }
    }

    private static IEnumerable<string> SplitListLiteral(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length < 2 || trimmed[0] != '[' || trimmed[^1] != ']')
        {
            yield break;
        }

        foreach (var part in trimmed[1..^1].Split(','))
        {
            var p = part.Trim().Trim('"', '\'');
            if (!string.IsNullOrEmpty(p))
            {
                yield return p;
            }
        }
    }

    private sealed record QueueItem(int Depth, IReadOnlyList<(string Id, string Type)> Batch);

    private sealed class IdComparer : IEqualityComparer<(string Id, string Type)>
    {
        public static readonly IdComparer Instance = new();

        public bool Equals((string Id, string Type) x, (string Id, string Type) y) =>
            string.Equals(x.Id, y.Id, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Type, y.Type, StringComparison.Ordinal);

        public int GetHashCode((string Id, string Type) obj) =>
            HashCode.Combine(obj.Id.ToLowerInvariant(), obj.Type);
    }
}
