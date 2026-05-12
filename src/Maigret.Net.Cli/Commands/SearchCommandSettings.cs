using System.ComponentModel;
using Spectre.Console.Cli;

namespace Maigret.Net.Cli;

/// <summary>
/// Command-line options for <see cref="SearchCommand"/>. Mirrors the relevant
/// flags of <c>maigret/maigret.py setup_arguments_parser</c>.
/// </summary>
public sealed class SearchCommandSettings : CommandSettings
{
    [CommandArgument(0, "<USERNAMES>")]
    [Description("One or more usernames (or other IDs) to search for")]
    public string[] Usernames { get; set; } = [];

    // -- HTTP behavior ---------------------------------------------------------

    [CommandOption("--timeout <SECONDS>")]
    [Description("Time in seconds to wait for response (default: 30)")]
    [DefaultValue(30)]
    public int Timeout { get; set; } = 30;

    [CommandOption("--retries <N>")]
    [Description("Number of retries on transient failures (default: 0)")]
    [DefaultValue(0)]
    public int Retries { get; set; }

    [CommandOption("-n|--max-connections <N>")]
    [Description("Maximum concurrent connections (default: 100)")]
    [DefaultValue(100)]
    public int MaxConnections { get; set; } = 100;

    [CommandOption("--proxy <URL>")]
    [Description("Generic proxy URL (HTTP or SOCKS5)")]
    public string? Proxy { get; set; }

    [CommandOption("--tor-proxy <URL>")]
    [Description("Tor SOCKS5 proxy URL (default: socks5://127.0.0.1:9050)")]
    public string? TorProxy { get; set; }

    [CommandOption("--i2p-proxy <URL>")]
    [Description("I2P proxy URL (default: http://127.0.0.1:4444)")]
    public string? I2pProxy { get; set; }

    [CommandOption("--cookies-jar-file <FILE>")]
    [Description("Mozilla-format cookies file for authenticated sites")]
    public string? CookiesJarFile { get; set; }

    // -- search behavior -------------------------------------------------------

    [CommandOption("--no-recursion")]
    [Description("Disable recursive search by extracted IDs")]
    public bool NoRecursion { get; set; }

    [CommandOption("--no-extracting")]
    [Description("Disable profile data extraction")]
    public bool NoExtracting { get; set; }

    [CommandOption("--id-type <TYPE>")]
    [Description("Identifier type (default: username; also gaia_id, vk_id, …)")]
    [DefaultValue("username")]
    public string IdType { get; set; } = "username";

    [CommandOption("--permute")]
    [Description("Permute usernames with separators ('_', '-', '.')")]
    public bool Permute { get; set; }

    [CommandOption("--db <PATH_OR_URL>")]
    [Description("Custom data.json file or URL (default: embedded resource)")]
    public string? Db { get; set; }

    [CommandOption("--with-domains")]
    [Description("Also probe domain names (DNS-only checks)")]
    public bool WithDomains { get; set; }

    // -- site selection --------------------------------------------------------

    [CommandOption("-a|--all-sites")]
    [Description("Scan all sites in the database (slow)")]
    public bool AllSites { get; set; }

    [CommandOption("--top-sites <N>")]
    [Description("Scan top N sites by Alexa rank (default: 500)")]
    [DefaultValue(500)]
    public int TopSites { get; set; } = 500;

    [CommandOption("--tags <CSV>")]
    [Description("Comma-separated tag filter (e.g. 'social,photo,us')")]
    public string? Tags { get; set; }

    [CommandOption("--ignore <CSV>")]
    [Description("Comma-separated list of site names to skip")]
    public string? Ignore { get; set; }

    // -- output ----------------------------------------------------------------

    [CommandOption("-o|--folderoutput <DIR>")]
    [Description("Folder for report files (default: ./reports)")]
    public string? FolderOutput { get; set; }

    [CommandOption("--txt")]
    [Description("Generate plain-text report")]
    public bool Txt { get; set; }

    [CommandOption("--csv")]
    [Description("Generate CSV report")]
    public bool Csv { get; set; }

    [CommandOption("--json")]
    [Description("Generate JSON report")]
    public bool Json { get; set; }

    [CommandOption("--html")]
    [Description("Generate HTML report")]
    public bool Html { get; set; }

    [CommandOption("--markdown")]
    [Description("Generate Markdown report")]
    public bool Markdown { get; set; }

    [CommandOption("--print-not-found")]
    [Description("Also print sites where the username was not found")]
    public bool PrintNotFound { get; set; }

    [CommandOption("--print-errors")]
    [Description("Also print sites that returned errors")]
    public bool PrintErrors { get; set; }

    [CommandOption("--no-color")]
    [Description("Disable colored output")]
    public bool NoColor { get; set; }
}
