// Recursive search knobs. Mirrors the Python defaults — depth-limited fan-out
// triggered by IDs harvested from claimed accounts.
namespace Maigret.Net.Core.RecursiveSearch;

/// <summary>
/// Options controlling recursive identifier expansion.
/// </summary>
public sealed class RecursiveSearchOptions
{
    /// <summary>Hard cap on recursion depth. <c>0</c> disables recursion.</summary>
    public int MaxDepth { get; init; } = 3;

    /// <summary>
    /// Maximum number of new identifiers expanded per parent result. Guards
    /// against pathological extractor output blowing up the queue.
    /// </summary>
    public int MaxIdsPerResult { get; init; } = 16;
}
