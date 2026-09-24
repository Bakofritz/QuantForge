using QuantForge.Core;

namespace QuantForge.Storage;

public interface IStrategyGovernanceStore
{
    Task SaveQuarantinedSourceAsync(StrategySourceArtifact artifact, string sourceText, StrategyAuditReport audit, CancellationToken cancellationToken = default);
    Task<QuarantinedStrategyRecord?> LoadQuarantinedSourceAsync(string sourceId, CancellationToken cancellationToken = default);
    Task SaveSelectionAsync(StrategyComponentSelection selection, CancellationToken cancellationToken = default);
    Task<StrategyComponentSelection?> LoadSelectionAsync(string sourceId, CancellationToken cancellationToken = default);
    Task SaveCanonicalModelAsync(CanonicalStrategyModel model, CancellationToken cancellationToken = default);
    Task<CanonicalStrategyModel?> LoadCanonicalModelAsync(string strategyId, CancellationToken cancellationToken = default);
    Task AppendLineageAsync(StrategyLineageEvent lineageEvent, CancellationToken cancellationToken = default);
    Task SaveCanonicalSemanticsAsync(CanonicalStrategySemantics semantics, CancellationToken cancellationToken = default);
    Task<CanonicalStrategySemantics?> LoadCanonicalSemanticsAsync(string strategyId, CancellationToken cancellationToken = default);
}

public sealed record QuarantinedStrategyRecord(StrategySourceArtifact Artifact, string SourceText, StrategyAuditReport Audit);
public sealed record StrategyLineageEvent(string LineageId, string SourceId, string EventType, string SubjectFingerprint, string ParentFingerprint, DateTimeOffset CreatedAt);
