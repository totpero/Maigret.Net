// Embedded resources shipped with Maigret.Net.Reports.Scriban — the default
// HTML report template.
namespace Maigret.Net.Reports.Scriban;

public static class ScribanResources
{
    /// <summary>Resource name for the default HTML report template.</summary>
    public const string DefaultHtmlTemplateResourceName = "Maigret.Net.Reports.Scriban.Resources.simple_report.html.scriban";

    /// <summary>Returns the contents of the default HTML report template.</summary>
    public static string GetDefaultHtmlTemplate()
    {
        var asm = typeof(ScribanResources).Assembly;
        return Maigret.Net.Core.MaigretResources.ReadStringFrom(asm, DefaultHtmlTemplateResourceName);
    }
}
