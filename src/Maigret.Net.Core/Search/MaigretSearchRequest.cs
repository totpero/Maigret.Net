using Maigret.Net.Core.Checkers;
using Microsoft.Extensions.Logging;

namespace Maigret.Net.Core.Search;

/// <summary>
/// Bundle of inputs accepted by <see cref="MaigretSearchEngine.SearchAsync"/>. Modeled as a
/// single parameter object to honor AV1561 (≤ 3 method parameters) and to make
/// downstream additions non-breaking.
/// </summary>
public sealed class MaigretSearchRequest
{
    /// <summary>Identifiers to search for. Whitespace-only entries are dropped.</summary>
    public required IEnumerable<string> Usernames { get; init; }

    /// <summary>Site database to query.</summary>
    public required MaigretDatabase Database { get; init; }

    /// <summary>Persistable configuration (timeout, max connections, …).</summary>
    public required Settings Settings { get; init; }

    /// <summary>Runtime site filter (tags, names, top-N).</summary>
    public SearchFilter? Filter { get; init; }

    /// <summary>Activation provider; defaults to a no-op.</summary>
    public IActivationProvider? Activation { get; init; }

    /// <summary>ID extractor; defaults to a no-op.</summary>
    public IIdExtractor? Extractor { get; init; }

    /// <summary>Optional progress notifier.</summary>
    public QueryNotify? Notify { get; init; }

    /// <summary>Optional logger.</summary>
    public ILogger? Logger { get; init; }

    /// <summary>
    /// Optional pre-built checker dictionary; the orchestrator owns and disposes
    /// the dictionary it builds itself, but callers retain ownership of any they pass in.
    /// </summary>
    public IDictionary<string, ICheckerBase>? Checkers { get; init; }
}
