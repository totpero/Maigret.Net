// One-call convenience entry point. Mirrors the appeal of `await maigret(...)`
// in Python: pass a username and start consuming results with `await foreach`.
namespace Maigret.Net.Core;

/// <summary>
/// Static factory for the simplest use case: search a single username with
/// embedded defaults and stream results.
/// </summary>
public static class MaigretFactory
{
    private static readonly Lazy<MaigretDatabase> EmbeddedDatabase =
        new(MaigretResources.LoadEmbeddedDatabase);

    /// <summary>Searches the embedded site database for <paramref name="username"/>.</summary>
    public static IAsyncEnumerable<MaigretCheckResult> SearchAsync(
        string username,
        Settings? settings = null,
        SearchFilter? filter = null,
        CancellationToken cancellationToken = default) =>
        SearchManyAsync(new[] { username }, settings, filter, cancellationToken);

    /// <summary>Searches for multiple usernames over the embedded site database.</summary>
    public static IAsyncEnumerable<MaigretCheckResult> SearchManyAsync(
        IEnumerable<string> usernames,
        Settings? settings = null,
        SearchFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usernames);

        var request = new MaigretSearchRequest
        {
            Usernames = usernames,
            Database = EmbeddedDatabase.Value,
            Settings = settings ?? Settings.LoadFromEmbedded(),
            Filter = filter,
        };
        return MaigretSearchEngine.SearchAsync(request, cancellationToken);
    }
}
