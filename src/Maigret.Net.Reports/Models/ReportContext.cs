// Input model for every IReportWriter. Mirrors the dictionary that Python's
// `report.py` builds (`username`, `results`, `generated_at`, `id_type`, …)
// but as a strongly-typed snapshot.

using System.Globalization;
using Maigret.Net.Core;

namespace Maigret.Net.Reports;

/// <summary>
/// Snapshot of search results and metadata fed to <see cref="IReportWriter"/>.
/// Multiple writers operating on the same context produce different file formats.
/// </summary>
public sealed class ReportContext
{
    public ReportContext(
        string username,
        IEnumerable<MaigretCheckResult> results,
        string idType = "username",
        DateTimeOffset? generatedAt = null,
        IReadOnlyDictionary<string, string>? extras = null)
    {
        Username = username;
        Results = results.ToList();
        IdType = idType;
        GeneratedAt = generatedAt ?? DateTimeOffset.Now;
        Extras = extras ?? new Dictionary<string, string>(0);
    }

    /// <summary>Identifier that was searched for.</summary>
    public string Username { get; }

    /// <summary>All probed results (all statuses).</summary>
    public IReadOnlyList<MaigretCheckResult> Results { get; }

    /// <summary>Identifier type (default <c>username</c>).</summary>
    public string IdType { get; }

    /// <summary>Timestamp embedded into the report header.</summary>
    public DateTimeOffset GeneratedAt { get; }

    /// <summary>Free-form extras (e.g. CLI flags) made available to template engines.</summary>
    public IReadOnlyDictionary<string, string> Extras { get; }

    /// <summary>Filtered claimed accounts.</summary>
    public IEnumerable<MaigretCheckResult> Claimed => Results.Where(r => r.Status == MaigretCheckStatus.Claimed);

    /// <summary>Total number of probes (all statuses).</summary>
    public int TotalChecked => Results.Count;

    /// <summary>Number of claimed accounts.</summary>
    public int TotalFound => Claimed.Count();

    /// <summary>Generated-at as the canonical formatted string used by Python reports.</summary>
    public string GeneratedAtIso8601 =>
        GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
}
