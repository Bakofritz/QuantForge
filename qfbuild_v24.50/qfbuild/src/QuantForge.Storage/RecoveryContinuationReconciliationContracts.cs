using QuantForge.Core;

namespace QuantForge.Storage;

public enum RecoveryContinuationReconciliationDecision
{
    ReviewRequired,
    ExternalCompletionConfirmed,
    Aborted,
    ReplayAuthorizationRequested
}

/// <summary>Durable record describing an ambiguous Prepared-only continuation operation.</summary>
public sealed record RecoveryContinuationReconciliationRecord(
    string DecisionFingerprint,
    string OperationFingerprint,
    string JobId,
    long Sequence,
    int Cursor,
    string StateHash,
    RecoveryContinuationReconciliationDecision Decision,
    string EvidenceFingerprint,
    string Reason,
    DateTimeOffset RecordedAt,
    string? ResolvedBy,
    DateTimeOffset? ResolvedAt);

public interface IRecoveryContinuationReconciliationStore
{
    Task<RecoveryContinuationReconciliationRecord?> LoadAsync(string operationFingerprint, CancellationToken cancellationToken = default);
    Task RecordAmbiguousAsync(RecoveryContinuationReconciliationRecord record, CancellationToken cancellationToken = default);
}

/// <summary>
/// Explicit reconciliation boundary. Recording a decision never authorizes automatic callback replay.
/// </summary>
public static class RecoveryContinuationReconciliationPolicy
{
    public const string AmbiguousPreparedCode = "RECOVERY_CONTINUATION_AMBIGUOUS_IN_FLIGHT_OPERATION";
    public const string ReplayStillBlockedCode = "RECOVERY_CONTINUATION_REPLAY_REQUIRES_EXPLICIT_EXTERNAL_RECONCILIATION";

    public static RecoveryContinuationReconciliationRecord CreateReviewRecord(
        RecoveryContinuationOperation operation,
        string evidenceFingerprint,
        string reason,
        DateTimeOffset now) =>
        new(
            ResearchFingerprint.Sha256($"QF-RECOVERY-RECONCILIATION|{operation.OperationFingerprint}|{evidenceFingerprint}"),
            operation.OperationFingerprint,
            operation.JobId,
            operation.Sequence,
            operation.Cursor,
            operation.StateHash,
            RecoveryContinuationReconciliationDecision.ReviewRequired,
            evidenceFingerprint,
            reason,
            now,
            null,
            null);
}
