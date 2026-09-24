namespace QuantForge.Core;

/// <summary>Point-in-time view of a bar that is valid at the observation timestamp.</summary>
public sealed record CausalBarView(
    DateTimeOffset Start,
    DateTimeOffset End,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    bool IsComplete);

public sealed record CanonicalDataGateResult(
    bool Allowed,
    string DatasetFingerprint,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
