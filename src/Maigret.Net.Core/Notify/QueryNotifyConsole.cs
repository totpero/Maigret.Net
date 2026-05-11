namespace Maigret.Net.Core.Notify;

/// <summary>
/// Console notifier. Mirrors <c>maigret.notify.QueryNotifyPrint</c>:
/// prints search start, per-result lines (with <c>[+] / [-] / [?]</c> markers),
/// and a final message.
/// </summary>
public class QueryNotifyConsole : QueryNotify
{
    public QueryNotifyConsole(
        bool verbose = false,
        bool printFoundOnly = false,
        bool skipCheckErrors = false,
        bool color = true,
        bool silent = false)
    {
        Verbose = verbose;
        PrintFoundOnly = printFoundOnly;
        SkipCheckErrors = skipCheckErrors;
        Color = color;
        Silent = silent;
    }

    public bool Verbose { get; }
    public bool PrintFoundOnly { get; }
    public bool SkipCheckErrors { get; }
    public bool Color { get; }
    public bool Silent { get; }

    public override void Start(string? message = null, string idType = "username")
    {
        if (Silent)
        {
            return;
        }

        if (Color)
        {
            WriteColoredStart(message, idType);
        }
        else
        {
            Console.WriteLine($"[*] Checking {idType} {message} on:");
        }
    }

    public override void Update(MaigretCheckResult result, bool isSimilar = false)
    {
        base.Update(result, isSimilar);
        if (Silent)
        {
            return;
        }

        var idsTree = result.IdsData is { Count: > 0 }
            ? MaigretUtilities.GetDictAsciiTree(result.IdsData.Select(kv => (kv.Key, kv.Value)), prepend: " ")
            : string.Empty;

        switch (result.Status)
        {
            case MaigretCheckStatus.Claimed:
            {
                var color = isSimilar ? ConsoleColor.Blue : ConsoleColor.Green;
                var symbol = isSimilar ? "?" : "+";
                Print(symbol, result.SiteName, color, color, result.SiteUrlUser + idsTree);
                break;
            }
            case MaigretCheckStatus.Available when !PrintFoundOnly:
                Print("-", result.SiteName, ConsoleColor.Red, ConsoleColor.Yellow, "Not found!" + idsTree);
                break;
            case MaigretCheckStatus.Unknown when !SkipCheckErrors:
                Print("?", result.SiteName, ConsoleColor.Red, ConsoleColor.Red,
                    (result.Error?.ToString() ?? "Unknown error") + idsTree);
                break;
            case MaigretCheckStatus.Illegal when !PrintFoundOnly:
                Print("-", result.SiteName, ConsoleColor.Red, ConsoleColor.Yellow,
                    "Illegal Username Format For This Site!" + idsTree);
                break;
        }
    }

    public void Success(string message, string symbol = "+") =>
        ColoredLine(ConsoleColor.Green, $"[{symbol}] {message}");

    public void Warning(string message, string symbol = "-") =>
        ColoredLine(ConsoleColor.Yellow, $"[{symbol}] {message}");

    public void Info(string message, string symbol = "*") =>
        ColoredLine(ConsoleColor.Blue, $"[{symbol}] {message}");

    private static void WriteColoredStart(string? message, string idType)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("*");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"] Checking {idType}");
        if (!string.IsNullOrEmpty(message))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($" {message}");
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" on:");
        Console.ResetColor();
    }

    private void ColoredLine(ConsoleColor color, string msg)
    {
        if (Color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine(msg);
        }
    }

    private void Print(string status, string siteName, ConsoleColor statusColor, ConsoleColor textColor, string appendix)
    {
        if (!Color)
        {
            Console.WriteLine($"[{status}] {siteName}: {appendix}");
            return;
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("[");
        Console.ForegroundColor = statusColor;
        Console.Write(status);
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("]");
        Console.ForegroundColor = textColor;
        Console.Write($" {siteName}: ");
        Console.ResetColor();
        Console.WriteLine(appendix);
    }
}
