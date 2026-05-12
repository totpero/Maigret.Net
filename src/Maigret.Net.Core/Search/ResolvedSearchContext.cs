using Microsoft.Extensions.Logging;

namespace Maigret.Net.Core.Search;

/// <summary>
/// Internal, fully-resolved view over a <see cref="MaigretSearchRequest"/>.
/// Constructed once at the entry of <see cref="MaigretSearchEngine.SearchAsync"/> so the
/// downstream helpers don't have to handle nulls or recompute defaults.
/// </summary>
internal sealed class ResolvedSearchContext
{
    public required IReadOnlyList<string> Usernames { get; init; }
    public required IReadOnlyList<MaigretSite> Sites { get; init; }
    public required IDictionary<string, ICheckerBase> Checkers { get; init; }
    public required bool OwnsCheckers { get; init; }
    public required QueryOptions Options { get; init; }
    public required SearchFilter Filter { get; init; }
    public required IActivationProvider Activation { get; init; }
    public required IIdExtractor Extractor { get; init; }
    public required ILogger Logger { get; init; }
    public required Settings Settings { get; init; }
    public QueryNotify? Notify { get; init; }
}
