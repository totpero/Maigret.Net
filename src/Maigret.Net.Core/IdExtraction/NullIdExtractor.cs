namespace Maigret.Net.Core.IdExtraction;

/// <summary>
/// Default no-op extractor used when nothing is registered.
/// </summary>
public sealed class NullIdExtractor : IIdExtractor
{
    public static readonly NullIdExtractor Instance = new();

    private static readonly IReadOnlyDictionary<string, string> Empty =
        new Dictionary<string, string>(0);

    public IReadOnlyDictionary<string, string> Extract(string htmlText, MaigretSite site) => Empty;
}
