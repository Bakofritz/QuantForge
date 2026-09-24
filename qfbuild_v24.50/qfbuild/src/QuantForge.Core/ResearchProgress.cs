namespace QuantForge.Core;

/// <summary>Durable, fingerprint-bound progress for a bounded research operation.</summary>
public sealed record ResearchProgress(
    string JobId,
    string Operation,
    int Cursor,
    int Total,
    string DatasetFingerprint,
    string ConfigurationFingerprint,
    string EngineFingerprint,
    string AggregationState,
    string ProgressHash,
    DateTimeOffset SavedAt);
