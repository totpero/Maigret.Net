namespace Maigret.Net.Core.Constants;

/// <summary>
/// Library-wide constants and default values.
/// </summary>
public static class MaigretDefaults
{
    /// <summary>
    /// Named <see cref="System.Net.Http.HttpClient"/> registered by <c>AddMaigret</c>.
    /// </summary>
    public const string HttpClientName = "Maigret.Net";

    /// <summary>
    /// Library version (kept in sync with the assembly version via <c>Directory.Build.props</c>).
    /// </summary>
    public const string Version = "0.1.0";
}
