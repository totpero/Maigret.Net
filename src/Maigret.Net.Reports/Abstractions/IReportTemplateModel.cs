namespace Maigret.Net.Reports;

/// <summary>
/// Minimal model fed to template engines. Members are exposed as plain dictionary
/// keys so engines that look things up by name (Scriban, Liquid, …) "just work".
/// </summary>
public interface IReportTemplateModel
{
    public string Username { get; }
    public string IdType { get; }
    public string GeneratedAt { get; }
    public int TotalChecked { get; }
    public int TotalFound { get; }
    public IReadOnlyList<IDictionary<string, object?>> Accounts { get; }
    public IReadOnlyList<IDictionary<string, object?>> TagSummary { get; }
    public IReadOnlyList<IDictionary<string, object?>> CountrySummary { get; }
    public IReadOnlyDictionary<string, string> Extras { get; }
}
