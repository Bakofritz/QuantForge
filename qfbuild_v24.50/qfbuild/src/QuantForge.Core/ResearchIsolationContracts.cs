namespace QuantForge.Core;

/// <summary>Immutable identity for one isolated research case. Mutable execution state must never be shared between contexts.</summary>
public sealed record ResearchContextIdentity(
    string ContextId,
    string JobId,
    string DatasetId,
    string DatasetFingerprint,
    string StrategyId,
    string StrategyFingerprint,
    string Instrument,
    string Timeframe,
    string ConfigurationFingerprint);

/// <summary>Explicit isolation boundary for strategy, position, ledger and evidence state.</summary>
public sealed record ResearchContextDescriptor(
    ResearchContextIdentity Identity,
    DateTimeOffset CreatedAt,
    string EngineFingerprint);

public sealed class ResearchContextState
{
    private readonly Dictionary<string, decimal> _strategyValues = new(StringComparer.Ordinal);
    private readonly List<string> _evidenceReferences = new();

    public ResearchContextState(ResearchContextDescriptor descriptor)
        => Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));

    public ResearchContextDescriptor Descriptor { get; }
    public int Position { get; private set; }
    public IReadOnlyDictionary<string, decimal> StrategyValues => _strategyValues;
    public IReadOnlyList<string> EvidenceReferences => _evidenceReferences.AsReadOnly();

    public void SetPosition(int position) => Position = position;
    public void SetStrategyValue(string key, decimal value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _strategyValues[key] = value;
    }
    public void AddEvidenceReference(string reference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        _evidenceReferences.Add(reference);
    }
}

public static class ResearchContextFingerprint
{
    public static string Create(ResearchContextIdentity identity) =>
        ResearchFingerprint.Sha256(string.Join("|", new[]
        {
            identity.JobId, identity.DatasetId, identity.DatasetFingerprint,
            identity.StrategyId, identity.StrategyFingerprint, identity.Instrument,
            identity.Timeframe, identity.ConfigurationFingerprint
        }));
}
