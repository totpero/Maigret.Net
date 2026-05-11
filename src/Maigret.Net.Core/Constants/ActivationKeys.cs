namespace Maigret.Net.Core.Constants;

/// <summary>
/// Well-known keys read from a site's <c>activation</c> object in <c>data.json</c>.
/// </summary>
public static class ActivationKeys
{
    /// <summary>Activator name (e.g. <c>twitter</c>, <c>vimeo</c>, <c>onlyfans</c>).</summary>
    public const string Method = "method";

    /// <summary>URL to fetch when running activation.</summary>
    public const string Url = "url";

    /// <summary>JSON path of the token in the activation response (Twitter).</summary>
    public const string Src = "src";

    /// <summary>Substrings whose presence in a response triggers re-activation.</summary>
    public const string Marks = "marks";

    // OnlyFans-specific
    public const string StaticParam = "static_param";
    public const string ChecksumIndexes = "checksum_indexes";
    public const string ChecksumConstant = "checksum_constant";
    public const string Format = "format";
}
