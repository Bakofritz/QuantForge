using QuantForge.Core;

namespace QuantForge.Storage;

public sealed record RecoveryContinuationRequest(
    string BatchId,
    string ContextId,
    string JobId,
    ResearchWorkerIdentity Worker,
    TimeSpan LeaseDuration,
    string ExpectedDatasetFingerprint,
    string ExpectedConfigurationFingerprint,
    string ExpectedEngineFingerprint);

public enum RecoveryContinuationState
{
    Ready,
    ReceiptAlreadyCommitted,
    LeaseStillActive,
    CheckpointMissing,
    CheckpointFingerprintMismatch,
    CheckpointIntegrityFailure,
    JobStateNotRecoverable,
    CancellationRequested,
    ReconciliationBlocked
}

public sealed record RecoveryContinuationResult(
    RecoveryContinuationState State,
    string BatchId,
    string ContextId,
    string JobId,
    string WorkerFingerprint,
    long? CheckpointSequence,
    int? CheckpointCursor,
    string? CheckpointStateHash,
    string AuditFingerprint,
    string? FailureCode);
