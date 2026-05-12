namespace Maigret.Net.Core.Notify;

/// <summary>
/// Base notify object. Mirrors <c>maigret.notify.QueryNotify</c>.
/// All members are no-ops; subclasses (e.g. <see cref="QueryNotifyConsole"/>) override them.
/// </summary>
public class QueryNotify
{
    public MaigretCheckResult? Result { get; protected set; }

    public virtual void Start(string? message = null, string idType = "username") { }

    public virtual void Update(MaigretCheckResult result, bool isSimilar = false) => Result = result;

    public virtual void Finish(string? message = null) { }

    public override string ToString() => Result?.ToString() ?? string.Empty;
}
