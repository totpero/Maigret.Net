// Port of maigret/settings.py — settings model and JSON loader cascade.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Maigret.Net.Core;

/// <summary>
/// Top-level Maigret runtime settings. Mirrors <c>maigret.settings.Settings</c> 1:1 —
/// JSON property names match the Python defaults so embedded
/// <c>resources/settings.json</c> deserializes without remapping.
/// </summary>
public sealed class Settings
{
    // -- main maigret settings (order mirrors settings.py) ---------------------------------

    [JsonPropertyName("retries_count")]
    public int RetriesCount { get; set; }

    [JsonPropertyName("sites_db_path")]
    public string SitesDbPath { get; set; } = string.Empty;

    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 30;

    [JsonPropertyName("max_connections")]
    public int MaxConnections { get; set; } = 100;

    [JsonPropertyName("recursive_search")]
    public bool RecursiveSearch { get; set; } = true;

    [JsonPropertyName("info_extracting")]
    public bool InfoExtracting { get; set; } = true;

    [JsonPropertyName("cookie_jar_file")]
    public string? CookieJarFile { get; set; }

    [JsonPropertyName("ignore_ids_list")]
    public List<string> IgnoreIdsList { get; set; } = new();

    [JsonPropertyName("reports_path")]
    public string ReportsPath { get; set; } = "reports";

    [JsonPropertyName("proxy_url")]
    public string? ProxyUrl { get; set; }

    [JsonPropertyName("tor_proxy_url")]
    public string TorProxyUrl { get; set; } = "socks5://127.0.0.1:9050";

    [JsonPropertyName("i2p_proxy_url")]
    public string I2pProxyUrl { get; set; } = "http://127.0.0.1:4444";

    [JsonPropertyName("domain_search")]
    public bool DomainSearch { get; set; }

    [JsonPropertyName("scan_all_sites")]
    public bool ScanAllSites { get; set; }

    [JsonPropertyName("top_sites_count")]
    public int TopSitesCount { get; set; } = 500;

    [JsonPropertyName("scan_disabled_sites")]
    public bool ScanDisabledSites { get; set; }

    [JsonPropertyName("scan_sites_list")]
    public List<string> ScanSitesList { get; set; } = new();

    [JsonPropertyName("self_check_enabled")]
    public bool SelfCheckEnabled { get; set; }

    [JsonPropertyName("print_not_found")]
    public bool PrintNotFound { get; set; }

    [JsonPropertyName("print_check_errors")]
    public bool PrintCheckErrors { get; set; }

    [JsonPropertyName("colored_print")]
    public bool ColoredPrint { get; set; } = true;

    [JsonPropertyName("show_progressbar")]
    public bool ShowProgressbar { get; set; } = true;

    [JsonPropertyName("report_sorting")]
    public string ReportSorting { get; set; } = "default";

    [JsonPropertyName("json_report_type")]
    public string JsonReportType { get; set; } = string.Empty;

    [JsonPropertyName("txt_report")]
    public bool TxtReport { get; set; }

    [JsonPropertyName("csv_report")]
    public bool CsvReport { get; set; }

    [JsonPropertyName("xmind_report")]
    public bool XmindReport { get; set; }

    [JsonPropertyName("pdf_report")]
    public bool PdfReport { get; set; }

    [JsonPropertyName("html_report")]
    public bool HtmlReport { get; set; }

    [JsonPropertyName("graph_report")]
    public bool GraphReport { get; set; }

    [JsonPropertyName("md_report")]
    public bool MdReport { get; set; }

    [JsonPropertyName("openai_api_key")]
    public string OpenAiApiKey { get; set; } = string.Empty;

    [JsonPropertyName("openai_model")]
    public string OpenAiModel { get; set; } = "gpt-4o";

    [JsonPropertyName("openai_api_base_url")]
    public string OpenAiApiBaseUrl { get; set; } = "https://api.openai.com/v1";

    [JsonPropertyName("web_interface_port")]
    public int WebInterfacePort { get; set; } = 5000;

    [JsonPropertyName("no_autoupdate")]
    public bool NoAutoupdate { get; set; }

    [JsonPropertyName("db_update_meta_url")]
    public string DbUpdateMetaUrl { get; set; } =
        "https://raw.githubusercontent.com/soxoj/maigret/main/maigret/resources/db_meta.json";

    [JsonPropertyName("autoupdate_check_interval_hours")]
    public int AutoupdateCheckIntervalHours { get; set; } = 24;

    // -- submit mode ---------------------------------------------------------------

    [JsonPropertyName("presence_strings")]
    public List<string> PresenceStrings { get; set; } = new();

    [JsonPropertyName("supposed_usernames")]
    public List<string> SupposedUsernames { get; set; } = new();

    /// <summary>
    /// Returns the cascade of paths the Python version walks when no explicit
    /// list is provided: embedded resource, <c>~/.maigret/settings.json</c>,
    /// and <c>./settings.json</c>.
    /// </summary>
    public static IReadOnlyList<string> DefaultSettingsFilePaths
    {
        get
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return new[]
            {
                Path.Combine(home, ".maigret", "settings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "settings.json"),
            };
        }
    }

    /// <summary>
    /// Loads default settings from the embedded <c>resources/settings.json</c> resource,
    /// then layers any of <see cref="DefaultSettingsFilePaths"/> that exist on top.
    /// </summary>
    /// <param name="paths">Optional override for the list of files to layer.</param>
    /// <returns>A populated <see cref="Settings"/> instance.</returns>
    public static Settings LoadDefaults(IEnumerable<string>? paths = null)
    {
        var settings = LoadFromEmbedded();
        var pathList = (paths ?? DefaultSettingsFilePaths).ToArray();
        foreach (var path in pathList)
        {
            if (File.Exists(path))
            {
                settings.MergeFromFile(path);
            }
        }
        return settings;
    }

    /// <summary>
    /// Loads settings exclusively from the embedded <c>resources/settings.json</c>.
    /// </summary>
    public static Settings LoadFromEmbedded()
    {
        using var stream = MaigretResources.OpenStream(MaigretResources.SettingsJsonResourceName);
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<Settings>(json, JsonOptions)
            ?? throw new InvalidDataException("Embedded settings.json deserialized to null.");
    }

    /// <summary>
    /// Layers the contents of <paramref name="path"/> on top of this settings instance,
    /// overwriting any property present in the file.
    /// </summary>
    public Settings MergeFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        using var doc = JsonDocument.Parse(stream);
        return MergeFrom(doc.RootElement);
    }

    /// <summary>
    /// Layers the contents of a JSON object on top of this settings instance.
    /// </summary>
    public Settings MergeFrom(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("settings JSON must be an object.");
        }

        // Round-trip through serializer to apply the (sparse) update onto our PascalCase model.
        var json = element.GetRawText();
        var partial = JsonSerializer.Deserialize<Settings>(json, JsonOptions);
        if (partial is null)
        {
            return this;
        }

        foreach (var prop in element.EnumerateObject())
        {
            // Apply only fields that were actually present in the document.
            CopyProperty(prop.Name, partial);
        }

        return this;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private void CopyProperty(string snakeName, Settings other)
    {
        var prop = typeof(Settings).GetProperties()
            .FirstOrDefault(p => string.Equals(
                p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                 .OfType<JsonPropertyNameAttribute>()
                 .FirstOrDefault()?.Name,
                snakeName,
                StringComparison.Ordinal));

        if (prop is null || !prop.CanWrite)
        {
            return;
        }

        prop.SetValue(this, prop.GetValue(other));
    }
}
