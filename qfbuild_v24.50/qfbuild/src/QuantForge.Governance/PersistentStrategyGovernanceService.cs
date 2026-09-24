using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Governance;

/// <summary>Persists governed strategy acquisition lineage while preserving the no-execution boundary.</summary>
public sealed class PersistentStrategyGovernanceService
{
    private readonly StrategyAcquisitionService _acquisition;
    private readonly IStrategyGovernanceStore _store;
    public PersistentStrategyGovernanceService(IStrategyGovernanceStore store, StrategyAcquisitionService? acquisition = null) { _store = store; _acquisition = acquisition ?? new StrategyAcquisitionService(); }

    public async Task<QuarantinedStrategySource> AcquireAuditAndPersistAsync(string sourceId, string source, StrategySourceLanguage language, CancellationToken cancellationToken = default)
    {
        var q = _acquisition.AcquireAndAudit(sourceId, source, language);
        await _store.SaveQuarantinedSourceAsync(q.Artifact, q.SourceText, q.Audit, cancellationToken);
        await _store.AppendLineageAsync(new StrategyLineageEvent($"STRAT-ACQUIRED-{q.Artifact.ContentHash[..16]}", sourceId, "SOURCE_ACQUIRED_QUARANTINED", q.Artifact.ContentHash, "", q.Artifact.AcquiredAt), cancellationToken);
        await _store.AppendLineageAsync(new StrategyLineageEvent($"STRAT-AUDITED-{q.Audit.AuditFingerprint[..16]}", sourceId, "SOURCE_AUDITED", q.Audit.AuditFingerprint, q.Artifact.ContentHash, DateTimeOffset.UtcNow), cancellationToken);
        return q;
    }

    public async Task<StrategyComponentSelection> SelectAndPersistAsync(QuarantinedStrategySource quarantined, IEnumerable<string> selectedFeatureIds, CancellationToken cancellationToken = default)
    {
        var selection = _acquisition.CommitSelection(quarantined, selectedFeatureIds);
        await _store.SaveSelectionAsync(selection, cancellationToken);
        await _store.AppendLineageAsync(new StrategyLineageEvent($"STRAT-SELECTED-{selection.SelectionFingerprint[..16]}", selection.SourceId, "COMPONENTS_SELECTED", selection.SelectionFingerprint, quarantined.Audit.AuditFingerprint, DateTimeOffset.UtcNow), cancellationToken);
        return selection;
    }

    public async Task<CanonicalStrategyModel> CommitCanonicalModelAsync(QuarantinedStrategySource quarantined, StrategyComponentSelection selection, CancellationToken cancellationToken = default)
    {
        var model = _acquisition.BuildResearchModel(quarantined, selection);
        await _store.SaveCanonicalModelAsync(model, cancellationToken);
        await _store.AppendLineageAsync(new StrategyLineageEvent($"STRAT-MODEL-{model.ModelFingerprint[..16]}", model.SourceId, "CANONICAL_MODEL_COMMITTED", model.ModelFingerprint, selection.SelectionFingerprint, DateTimeOffset.UtcNow), cancellationToken);
        return model;
    }
}
