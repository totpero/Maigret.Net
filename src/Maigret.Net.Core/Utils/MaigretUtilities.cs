// Misc helpers ported from maigret/utils.py.

using System.Text;

namespace Maigret.Net.Core.Utils;

/// <summary>
/// Misc helpers ported from <c>maigret.utils</c>: user-agent rotation, random
/// username generator, ASCII tree printer.
/// </summary>
public static class MaigretUtilities
{
    /// <summary>
    /// Default user-agent pool. Mirrors <c>DEFAULT_USER_AGENTS</c> from the Python module.
    /// </summary>
    public static IReadOnlyList<string> DefaultUserAgents { get; } = new[]
    {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36",
    };

    private static readonly Random Random = Random.Shared;

    /// <summary>Picks a random user-agent from <see cref="DefaultUserAgents"/>.</summary>
    public static string GetRandomUserAgent() =>
        DefaultUserAgents[Random.Next(DefaultUserAgents.Count)];

    /// <summary>Generates a random lower-case ASCII username of the given length.</summary>
    public static string GenerateRandomUsername(int length = 10)
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyz";
        var buffer = new char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = alphabet[Random.Next(alphabet.Length)];
        }

        return new string(buffer);
    }

    /// <summary>
    /// Renders a list of <c>(field, value)</c> pairs (or plain strings) as a multiline
    /// box-drawing tree, mirroring <c>maigret.utils.get_dict_ascii_tree</c>.
    /// </summary>
    public static string GetDictAsciiTree(IEnumerable<(string Key, string Value)> items, string prepend = "")
    {
        const string branch = "├";
        const string lastBranch = "└";
        const string horizontal = "─";

        var list = items.ToList();
        if (list.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < list.Count; i++)
        {
            var (k, v) = list[i];
            var box = (i == list.Count - 1 ? lastBranch : branch) + horizontal;
            sb.Append('\n').Append(prepend).Append(box).Append(k).Append(": ").Append(v);
        }
        return sb.ToString();
    }
}
