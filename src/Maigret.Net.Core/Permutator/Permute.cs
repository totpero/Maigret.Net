// Port of maigret/permutator.py — combinatorial username permutator.
// Used by the CLI's --permute flag to expand a set of input fragments
// (e.g. first name, last name) into ranked candidate usernames.
//
// Original (MIT) by balestek — https://github.com/balestek

using System.Diagnostics.CodeAnalysis;

namespace Maigret.Net.Core.Permutator;

/// <summary>
/// Combinatorial permutator. Mirrors <c>maigret.permutator.Permute</c>.
/// </summary>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
    Justification = "Permutations<TItem> is a generic helper exposed on Permute<T> for API symmetry with the Python source; moving it to a non-generic sibling type would be a breaking change.")]
public sealed class Permute<T>
{
    /// <summary>Default separators applied between joined keys.</summary>
    public static readonly IReadOnlyList<string> DefaultSeparators = [string.Empty, "_", "-", "."];

    private readonly IReadOnlyList<KeyValuePair<string, T>> _elements;
    private readonly IReadOnlyList<string> _separators;

    public Permute(IEnumerable<KeyValuePair<string, T>> elements, IReadOnlyList<string>? separators = null)
    {
        ArgumentNullException.ThrowIfNull(elements);

        _elements = [.. elements];
        _separators = separators ?? DefaultSeparators;
    }

    public Permute(IDictionary<string, T> elements, IReadOnlyList<string>? separators = null)
        : this(elements?.AsEnumerable() ?? [], separators)
    {
    }

    /// <summary>Returns the dictionary of <c>permutation → originating value</c>.</summary>
    public Dictionary<string, T> Gather(PermuteMode mode = PermuteMode.Strict)
    {
        var result = new Dictionary<string, T>(StringComparer.Ordinal);
        if (_elements.Count == 0)
        {
            return result;
        }

        for (var size = 1; size <= _elements.Count; size++)
        {
            foreach (var subset in Permutations(_elements, size))
            {
                AddSubsetPermutations(subset, size, mode, result);
            }
        }
        return result;
    }

    private void AddSubsetPermutations(
        IReadOnlyList<KeyValuePair<string, T>> subset,
        int size,
        PermuteMode mode,
        Dictionary<string, T> result)
    {
        if (size == 1)
        {
            if (mode != PermuteMode.All)
            {
                return;
            }

            var key = subset[0].Key;
            result[key] = subset[0].Value;
            result["_" + key] = subset[0].Value;
            result[key + "_"] = subset[0].Value;
            return;
        }

        foreach (var separator in _separators)
        {
            var perm = string.Join(separator, subset.Select(kv => kv.Key));
            result[perm] = subset[0].Value;
            if (separator.Length == 0)
            {
                result["_" + perm] = subset[0].Value;
                result[perm + "_"] = subset[0].Value;
            }
        }
    }

    /// <summary>Yields every k-length permutation of <paramref name="source"/> (Python <c>itertools.permutations</c>).</summary>
    public static IEnumerable<IReadOnlyList<TItem>> Permutations<TItem>(IReadOnlyList<TItem> source, int length)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (length == 0)
        {
            yield return Array.Empty<TItem>();
            yield break;
        }
        if (length > source.Count)
        {
            yield break;
        }

        var indices = new int[length];
        var used = new bool[source.Count];
        foreach (var perm in Walk(source, indices, used, 0))
        {
            yield return perm;
        }
    }

    private static IEnumerable<IReadOnlyList<TItem>> Walk<TItem>(
        IReadOnlyList<TItem> source, int[] indices, bool[] used, int pos)
    {
        if (pos == indices.Length)
        {
            var snapshot = new TItem[indices.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                snapshot[i] = source[indices[i]];
            }

            yield return snapshot;
            yield break;
        }
        for (var i = 0; i < source.Count; i++)
        {
            if (used[i])
            {
                continue;
            }

            used[i] = true;
            indices[pos] = i;
            foreach (var p in Walk(source, indices, used, pos + 1))
            {
                yield return p;
            }

            used[i] = false;
        }
    }
}
