// Public contract for the recursive search orchestrator. The default
// implementation drives MaigretSearchEngine.SearchAsync repeatedly, but consumers can
// substitute their own (e.g. for offline replay, distributed runs, etc.).

namespace Maigret.Net.Core.RecursiveSearch;

/// <summary>
/// Streams claimed/available results from an initial set of usernames and any
/// identifiers harvested during the search.
/// </summary>
public interface IRecursiveSearchEngine
{
    /// <summary>
    /// Runs <paramref name="request"/> and follows up with newly-discovered
    /// identifiers up to <paramref name="recursive"/>.MaxDepth levels deep.
    /// </summary>
    public IAsyncEnumerable<MaigretCheckResult> SearchAsync(
        MaigretSearchRequest request,
        RecursiveSearchOptions? recursive = null,
        CancellationToken cancellationToken = default);
}
