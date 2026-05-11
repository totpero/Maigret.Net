namespace Maigret.Net.Core.Errors;

/// <summary>
/// One row in <see cref="CommonErrors.ExtractAndGroup"/>'s output.
/// </summary>
public readonly record struct ErrorStat(string Type, int Count, double Percent);
