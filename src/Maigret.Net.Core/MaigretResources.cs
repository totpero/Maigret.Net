// Helpers for loading resources embedded in the Maigret.Net assembly.
// The Python package keeps `data.json`, templates, and `settings.json` in
// `maigret/resources/`; we ship them as <EmbeddedResource> in the .csproj.

using System.Reflection;

namespace Maigret.Net.Core;

/// <summary>
/// Provides access to assets bundled inside the <c>Maigret.Net</c> assembly.
/// </summary>
public static class MaigretResources
{
    /// <summary>Default site database resource name.</summary>
    public const string DataJsonResourceName = "Maigret.Net.Core.Resources.data.json";

    /// <summary>Default settings resource name.</summary>
    public const string SettingsJsonResourceName = "Maigret.Net.Core.Resources.settings.json";

    /// <summary>Reads an embedded resource as a UTF-8 string.</summary>
    public static string ReadString(string resourceName)
    {
        using var stream = OpenStream(resourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Reads an embedded resource as a UTF-8 string from a specific assembly.
    /// </summary>
    public static string ReadStringFrom(System.Reflection.Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"Embedded resource '{resourceName}' not found in {assembly.GetName().Name}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Opens a stream over an embedded resource. The caller owns the stream.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown when the resource is missing.</exception>
    public static Stream OpenStream(string resourceName)
    {
        var asm = typeof(MaigretResources).Assembly;
        var stream = asm.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            var available = string.Join(", ", asm.GetManifestResourceNames());
            throw new FileNotFoundException(
                $"Embedded resource '{resourceName}' not found. Available: {available}");
        }
        return stream;
    }

    /// <summary>
    /// Loads the bundled <c>data.json</c> into a fresh <see cref="MaigretDatabase"/>.
    /// </summary>
    public static MaigretDatabase LoadEmbeddedDatabase()
    {
        using var stream = OpenStream(DataJsonResourceName);
        return new MaigretDatabase().LoadFromStream(stream);
    }
}
