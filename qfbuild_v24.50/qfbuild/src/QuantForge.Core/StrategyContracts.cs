namespace QuantForge.Core;

public enum StrategySourceLanguage { Unknown, NinjaScript, TradingViewPine, JavaScript, CSharp, Python }
public enum StrategySourceStatus { Quarantined, Audited, ComponentSelected, Rejected }
public enum StrategyFeatureCategory
{
    Indicators, Signals, Orders, RiskManagement, PlotsAndDrawings, Alerts,
    Timeframes, ExternalDependencies, NetworkAccess, FileSystemAccess,
    EnvironmentAccess, DynamicExecution, ProcessExecution, Authentication,
    DataAccess, Unknown
}

public sealed record StrategySourceArtifact(
    string SourceId,
    StrategySourceLanguage Language,
    string ContentHash,
    int CharacterCount,
    DateTimeOffset AcquiredAt,
    StrategySourceStatus Status);

public sealed record StrategyFeature(
    string FeatureId,
    StrategyFeatureCategory Category,
    string Name,
    string Evidence,
    bool IsSafetySensitive,
    bool IsSupportedByCanonicalModel);

public sealed record StrategyAuditReport(
    string SourceId,
    string ContentHash,
    StrategySourceLanguage Language,
    IReadOnlyList<StrategyFeature> Features,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> SafetySignals,
    IReadOnlyList<string> LicenseSignals,
    bool ExecutionAllowed,
    string AuditFingerprint,
    string FidelityDeclaration);

public sealed record StrategyComponentSelection(
    string SourceId,
    IReadOnlySet<string> SelectedFeatureIds,
    string SelectionFingerprint,
    bool CommitRequested);

public sealed record CanonicalStrategyModel(
    string StrategyId,
    string SourceId,
    string SourceFingerprint,
    IReadOnlyList<string> Indicators,
    IReadOnlyList<string> EntryRules,
    IReadOnlyList<string> ExitRules,
    IReadOnlyList<string> RiskRules,
    IReadOnlyList<string> TimeframeRules,
    IReadOnlyList<string> VisualElements,
    IReadOnlyList<string> Alerts,
    string ModelFingerprint,
    bool ResearchEligible,
    bool LiveDeploymentEligible);
