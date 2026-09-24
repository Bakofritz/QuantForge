namespace QuantForge.Storage;

public enum RecoveryExternalSideEffectEvidenceState
{
    Declared,
    Confirmed,
    Rejected
}

public sealed record RecoveryExternalSideEffectEvidence(
    string EvidenceFingerprint,
    string OperationFingerprint,
    string EffectType,
    string EffectFingerprint,
    RecoveryExternalSideEffectEvidenceState State,
    string SourceFingerprint,
    DateTimeOffset RecordedAt);

public interface IRecoveryExternalSideEffectEvidenceStore
{
    Task<RecoveryExternalSideEffectEvidence?> LoadAsync(string operationFingerprint, CancellationToken cancellationToken = default);
    Task RecordAsync(RecoveryExternalSideEffectEvidence evidence, CancellationToken cancellationToken = default);
}
