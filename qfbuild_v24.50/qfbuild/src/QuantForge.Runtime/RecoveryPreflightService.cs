using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record RecoveryPreflightResult(
    string PreflightFingerprint,
    string Decision,
    string Reason,
    string IntegrityFingerprint,
    string? TerminalReceiptFingerprint);

/// <summary>
/// Records the read-only recovery decision before checkpoint continuation. It never changes job state.
/// The durable receipt binds the decision to the integrity result and any already-known terminal receipt.
/// </summary>
public sealed class RecoveryPreflightService
{
    private readonly IRecoveryPreflightStore _store;
    private readonly IRecoveryAuditStore? _audits;
    private readonly ILocalEvidenceStore? _evidence;

    public RecoveryPreflightService(IRecoveryPreflightStore store, IRecoveryAuditStore? audits = null, ILocalEvidenceStore? evidence = null)
    {
        _store = store;
        _audits = audits;
        _evidence = evidence;
    }

    public async Task<RecoveryPreflightResult> RecordAsync(
        RecoveryContinuationRequest request,
        WorkerRecoveryIntegrityResult integrity,
        ResearchCheckpoint? checkpoint = null,
        JobLeaseRecord? acceptedLease = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(integrity);

        var decision = integrity.State == WorkerRecoveryIntegrityState.SafeToRecover ? "Allowed" : "Blocked";
        var acceptedLeaseFingerprint = acceptedLease is null ? null : ResearchFingerprint.Sha256($"{acceptedLease.JobId}|{acceptedLease.DeviceId}|{acceptedLease.Version}|{acceptedLease.AcquiredAt:O}|{acceptedLease.ExpiresAt:O}");
        var fingerprint = ResearchFingerprint.Sha256(
            $"{request.BatchId}|{request.ContextId}|{request.JobId}|{request.Worker.DeviceId}|{request.Worker.IdentityFingerprint}|{decision}|{integrity.Reason}|{integrity.ResultFingerprint}|{integrity.TerminalReceiptFingerprint}|{checkpoint?.Sequence}|{checkpoint?.Cursor}|{checkpoint?.StateHash}|{acceptedLease?.Version}|{acceptedLeaseFingerprint}");
        var receipt = new RecoveryPreflightReceipt(
            fingerprint, request.BatchId, request.ContextId, request.JobId, request.Worker.DeviceId,
            request.Worker.IdentityFingerprint, decision, integrity.Reason, integrity.ResultFingerprint,
            integrity.TerminalReceiptFingerprint, checkpoint?.Sequence, checkpoint?.Cursor, checkpoint?.StateHash, acceptedLease?.Version, acceptedLeaseFingerprint, DateTimeOffset.UtcNow);

        await _store.AppendRecoveryPreflightAsync(receipt, cancellationToken);

        string? auditFingerprint = null;
        if (_audits is not null)
        {
            auditFingerprint = ResearchFingerprint.Sha256($"{fingerprint}|RECOVERY_PREFLIGHT|{decision}");
            await _audits.AppendRecoveryAuditAsync(new RecoveryAuditRecord(
                auditFingerprint, request.BatchId, request.ContextId, request.JobId, request.Worker.IdentityFingerprint,
                "RECOVERY_PREFLIGHT", decision, integrity.Reason, integrity.TerminalReceiptFingerprint, DateTimeOffset.UtcNow), cancellationToken);
        }

        if (_evidence is not null)
        {
            var evidenceHash = ResearchFingerprint.Sha256($"{fingerprint}|{integrity.ResultFingerprint}|{auditFingerprint}");
            await _evidence.AppendEvidenceAsync($"recovery-preflight:{fingerprint}", evidenceHash, cancellationToken);
            await _evidence.AppendEventAsync($"event:recovery-preflight:{fingerprint}", request.JobId, "RECOVERY_PREFLIGHT", evidenceHash, cancellationToken);
        }

        return new RecoveryPreflightResult(fingerprint, decision, integrity.Reason, integrity.ResultFingerprint, integrity.TerminalReceiptFingerprint);
    }
}
