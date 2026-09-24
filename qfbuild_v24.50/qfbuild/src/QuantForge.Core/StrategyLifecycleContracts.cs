namespace QuantForge.Core;

public enum StrategyLifecycleState { Quarantined, Audited, Selected, Governed, Canonicalized, ResearchRegistered, Retired }
public enum StrategyCapability { StaticAudit, ReadOnlyResearch, ParameterOptimization, HistoricalMarketDataRead, MultiScriptBatchRead, LiveOrderSubmission }

public sealed record StrategyCapabilityManifest(
    string StrategyId,
    string SourceId,
    string ModelFingerprint,
    IReadOnlySet<StrategyCapability> GrantedCapabilities,
    IReadOnlySet<StrategyCapability> DeniedCapabilities,
    string ManifestFingerprint,
    DateTimeOffset IssuedAt,
    bool IsResearchOnly);

public sealed record StrategyProvenanceRecord(
    string SourceId,
    string ContentHash,
    StrategySourceLanguage Language,
    string AuditFingerprint,
    string SelectionFingerprint,
    string DecisionFingerprint,
    string CommitReceiptFingerprint,
    string ModelFingerprint,
    string? SemanticsFingerprint,
    DateTimeOffset RecordedAt,
    string ProvenanceFingerprint);

public sealed record StrategyRegistryEntry(
    string StrategyId,
    string SourceId,
    string ModelFingerprint,
    StrategyLifecycleState State,
    string ProvenanceFingerprint,
    string CapabilityManifestFingerprint,
    bool ResearchEligible,
    bool LiveDeploymentEligible,
    DateTimeOffset RegisteredAt);
