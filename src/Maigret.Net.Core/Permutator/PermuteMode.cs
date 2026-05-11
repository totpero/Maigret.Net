namespace Maigret.Net.Core.Permutator;

/// <summary>
/// Method modes for <see cref="Permute{T}.Gather"/>. Mirrors the Python
/// <c>"strict" or "all"</c> argument.
/// </summary>
public enum PermuteMode
{
    /// <summary>Only multi-element permutations (and underscore-padded variants of the empty join).</summary>
    Strict,

    /// <summary>Includes single-element permutations and their underscore-padded variants.</summary>
    All,
}
