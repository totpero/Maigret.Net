namespace Maigret.Net.Core.IdExtraction;

/// <summary>
/// Strategy interface for extracting structured profile data (full name, email,
/// linked accounts, etc.) from a successful response body.
/// </summary>
public interface IIdExtractor
{
    /// <summary>
    /// Returns the extracted <c>(field → value)</c> pairs, or an empty dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, string> Extract(string htmlText, MaigretSite site);
}
