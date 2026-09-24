namespace QuantForge.Core;

public enum CanonicalPriceSource { Close, Open, High, Low, TypicalPrice, MedianPrice }
public enum CanonicalComparison { GreaterThan, LessThan, CrossAbove, CrossBelow }
public enum CanonicalDirection { Long, Short, Flat }
public enum CanonicalExitKind { OpposingSignal, StopLoss, ProfitTarget, TrailingStop, TimeExit, SessionExit }

public sealed record CanonicalParameterDefinition(
    string ParameterId,
    string Name,
    decimal DefaultValue,
    decimal? Minimum,
    decimal? Maximum,
    decimal? Step,
    string Provenance,
    bool SourceDerived,
    string Fingerprint);

public sealed record CanonicalIndicatorSemantics(
    string IndicatorId,
    string Name,
    string Kind,
    CanonicalPriceSource PriceSource,
    IReadOnlyList<CanonicalParameterDefinition> Parameters,
    string Provenance,
    string Fingerprint);

public sealed record CanonicalEntrySemantics(
    string EntryId,
    CanonicalDirection Direction,
    string Trigger,
    IReadOnlyList<string> RequiredIndicators,
    string Timeframe,
    string Provenance,
    string Fingerprint);

public sealed record CanonicalExitSemantics(
    string ExitId,
    CanonicalExitKind Kind,
    CanonicalDirection Direction,
    string Expression,
    decimal? Value,
    string Provenance,
    string Fingerprint);

public sealed record CanonicalRiskSemantics(
    string RiskId,
    string Name,
    string Expression,
    decimal? Value,
    string Provenance,
    string Fingerprint);

public sealed record CanonicalStrategySemantics(
    string StrategyId,
    string ModelFingerprint,
    IReadOnlyList<CanonicalIndicatorSemantics> Indicators,
    IReadOnlyList<CanonicalEntrySemantics> Entries,
    IReadOnlyList<CanonicalExitSemantics> Exits,
    IReadOnlyList<CanonicalRiskSemantics> Risks,
    IReadOnlyList<string> UnsupportedSemantics,
    string SemanticsFingerprint);

public sealed record CanonicalIndicatorArtifact(
    string StrategyId,
    string IndicatorArtifactId,
    string SemanticsFingerprint,
    IReadOnlyList<string> IndicatorIds,
    string ArtifactFingerprint,
    string FidelityDeclaration);

public sealed record CanonicalStrategyExecutionPlan(
    string StrategyId,
    string SemanticsFingerprint,
    CanonicalDirection PrimaryDirection,
    int FastPeriod,
    int SlowPeriod,
    decimal PointValue,
    decimal CommissionPoints,
    bool IsSupported,
    IReadOnlyList<string> UnsupportedSemantics,
    string ExecutionPlanFingerprint,
    string FidelityDeclaration,
    string? RiskProfileFingerprint = null);
