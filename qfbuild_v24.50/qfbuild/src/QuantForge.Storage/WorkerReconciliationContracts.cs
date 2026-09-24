namespace QuantForge.Storage;

public sealed record JobLeaseRecord(string JobId, string DeviceId, DateTimeOffset AcquiredAt, DateTimeOffset ExpiresAt, long Version);

public interface ILeaseInspectionStore
{
    Task<JobLeaseRecord?> LoadLeaseAsync(string jobId, CancellationToken cancellationToken = default);
}

public sealed record RecoveryAuditRecord(
    string AuditFingerprint,
    string BatchId,
    string ContextId,
    string JobId,
    string WorkerFingerprint,
    string PreviousState,
    string ReconciliationState,
    string Reason,
    string? ReceiptFingerprint,
    DateTimeOffset RecordedAt);

public interface IRecoveryAuditStore
{
    Task AppendRecoveryAuditAsync(RecoveryAuditRecord record, CancellationToken cancellationToken = default);
}


public interface IRecoveryBindingReconciliationStore
{
    Task AppendRecoveryBindingReconciliationAtomicallyAsync(
        RecoveryAuditRecord audit,
        string evidenceId,
        string evidenceHash,
        string eventId,
        string eventJobId,
        string eventType,
        string eventPayloadHash,
        CancellationToken cancellationToken = default);
}


public interface IRecoveryBindingReconciliationVerificationStore
{
    Task<bool> VerifyRecoveryBindingReconciliationAsync(
        string auditFingerprint,
        string evidenceId,
        string eventId,
        string eventPayloadHash,
        CancellationToken cancellationToken = default);
}
