namespace QuantForge.Core;

/// <summary>Immutable declaration of the cases and identities that belong to one research batch.</summary>
public sealed record ResearchBatchManifestCase(
    string ContextId,
    string JobId,
    string DatasetId,
    string DatasetFingerprint,
    string StrategyId,
    string StrategyFingerprint,
    string Instrument,
    string Timeframe,
    string ConfigurationFingerprint,
    string EngineFingerprint);

public sealed record ResearchBatchManifest(
    string BatchId,
    string BatchFingerprint,
    IReadOnlyList<ResearchBatchManifestCase> Cases)
{
    public string ManifestFingerprint => ResearchFingerprint.Sha256(string.Join("||", new[]
    {
        BatchId, BatchFingerprint,
        string.Join("@@", Cases.OrderBy(x => x.ContextId, StringComparer.Ordinal).Select(x => string.Join("|", new[]
        {
            x.ContextId, x.JobId, x.DatasetId, x.DatasetFingerprint, x.StrategyId,
            x.StrategyFingerprint, x.Instrument, x.Timeframe, x.ConfigurationFingerprint, x.EngineFingerprint
        })))
    }));
}
