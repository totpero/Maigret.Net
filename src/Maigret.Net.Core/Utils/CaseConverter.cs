using System.Text;
using System.Text.RegularExpressions;

namespace Maigret.Net.Core.Utils;

/// <summary>
/// camelCase ↔ snake_case helpers. Mirrors <c>maigret.utils.CaseConverter</c>.
/// </summary>
public static class CaseConverter
{
    private static readonly Regex CamelToSnakeRegex = new("(?<!^)([A-Z])", RegexOptions.Compiled);

    /// <summary>Converts <c>camelCase</c> or <c>PascalCase</c> to <c>snake_case</c>.</summary>
    public static string CamelToSnake(string value) =>
        string.IsNullOrEmpty(value) ? value : CamelToSnakeRegex.Replace(value, "_$1").ToLowerInvariant();

    /// <summary>Converts <c>snake_case</c> to <c>camelCase</c>.</summary>
    public static string SnakeToCamel(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var parts = value.Split('_');
        if (parts.Length == 1)
        {
            return parts[0];
        }

        var sb = new StringBuilder(parts[0]);
        for (var i = 1; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Length == 0)
            {
                continue;
            }

            sb.Append(char.ToUpperInvariant(p[0]));
            if (p.Length > 1)
            {
                sb.Append(p, 1, p.Length - 1);
            }
        }
        return sb.ToString();
    }
}
