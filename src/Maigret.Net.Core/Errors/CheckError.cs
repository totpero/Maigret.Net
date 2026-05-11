namespace Maigret.Net.Core.Errors;

/// <summary>
/// Error info attached to a <see cref="MaigretCheckResult"/> when the site responded
/// with bot protection / captcha / censorship / etc.
/// Mirrors <c>maigret.errors.CheckError</c>.
/// </summary>
public sealed class CheckError
{
    public CheckError(string type, string desc = "")
    {
        Type = type;
        Desc = desc;
    }

    public string Type { get; }

    public string Desc { get; }

    public override string ToString() =>
        string.IsNullOrEmpty(Desc) ? $"{Type} error" : $"{Type} error: {Desc}";
}
