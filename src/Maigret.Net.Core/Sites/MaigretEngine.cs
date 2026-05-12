using System.Text.Json;

namespace Maigret.Net.Core.Sites;

/// <summary>
/// Site engine definition: a reusable bundle of detection rules that can be
/// applied to multiple sites (e.g. <c>XenForo</c>, <c>phpBB</c>, <c>Discourse</c>).
/// Mirrors <c>maigret.sites.MaigretEngine</c>.
/// </summary>
public sealed class MaigretEngine
{
    public MaigretEngine(string name, JsonElement data)
    {
        Name = name;
        if (data.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (data.TryGetProperty("site", out var site) && site.ValueKind == JsonValueKind.Object)
        {
            Site = site.Clone();
        }

        if (data.TryGetProperty("presenseStrs", out var ps) && ps.ValueKind == JsonValueKind.Array)
        {
            PresenseStrs = ReadStringList(ps);
        }
        else if (data.TryGetProperty("presenceStrs", out var pc) && pc.ValueKind == JsonValueKind.Array)
        {
            PresenseStrs = ReadStringList(pc);
        }
    }

    /// <summary>Engine name (e.g. <c>XenForo</c>).</summary>
    public string Name { get; }

    /// <summary>
    /// Engine site rules; merged into hosting <see cref="MaigretSite"/> instances at load time.
    /// </summary>
    public JsonElement Site { get; }

    /// <summary>Engine-level presence strings.</summary>
    public IReadOnlyList<string> PresenseStrs { get; } = [];

    private static List<string> ReadStringList(JsonElement element)
    {
        var list = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                list.Add(item.GetString() ?? string.Empty);
            }
        }
        return list;
    }
}
