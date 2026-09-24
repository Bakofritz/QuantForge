namespace QuantForge.Storage;

public sealed record RecoveryPreflightReceipt(
    string PreflightFingerprint,
    string BatchId,
    string ContextId,
    string JobId,
    string WorkerDeviceId,
    string WorkerFingerprint,
    string Decision,
    string Reason,
    string IntegrityFingerprint,
    string? TerminalReceiptFingerprint,
    long? CheckpointSequence,
    int? CheckpointCursor,
    string? CheckpointStateHash,
    long? AcceptedLeaseVersion,
    string? AcceptedLeaseFingerprint,
    DateTimeOffset RecordedAt);

public interface IRecoveryPreflightStore
{
    Task AppendRecoveryPreflightAsync(RecoveryPreflightReceipt receipt, CancellationToken cancellationToken = default);
    Task<RecoveryPreflightReceipt?> LoadRecoveryPreflightAsync(string preflightFingerprint, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecoveryPreflightReceipt>> LoadRecoveryPreflightsForJobAsync(string jobId, CancellationToken cancellationToken = default);
}
