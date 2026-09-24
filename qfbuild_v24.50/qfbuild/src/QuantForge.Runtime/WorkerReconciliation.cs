using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public enum RecoveryReconciliationState { NoAction, LeaseActive, StaleWorker, ReceiptAlreadyCommitted, RecoveryRequired }

public sealed record RecoveryReconciliationResult(
    string BatchId,
    string ContextId,
    string JobId,
    RecoveryReconciliationState State,
    string Reason,
    string? ReceiptFingerprint,
    string AuditFingerprint);

/// <summary>Conservative reconciliation boundary. It detects stale ownership and receipt/state disagreement but never invents execution progress.</summary>
public sealed class WorkerReconciliationService
{
    private readonly IResearchJobStore _jobs;
    private readonly ILeaseInspectionStore _leases;
    private readonly IResourceReceiptStore _receipts;
    private readonly IRecoveryAuditStore _audits;
    private readonly ILocalEvidenceStore? _evidence;

    public WorkerReconciliationService(IResearchJobStore jobs, ILeaseInspectionStore leases, IResourceReceiptStore receipts, IRecoveryAuditStore audits, ILocalEvidenceStore? evidence = null)
    { _jobs = jobs; _leases = leases; _receipts = receipts; _audits = audits; _evidence = evidence; }

    public async Task<RecoveryReconciliationResult> ReconcileAsync(string batchId, string contextId, string jobId, string workerFingerprint, CancellationToken cancellationToken = default)
    {
        var job = await _jobs.LoadJobAsync(jobId, cancellationToken) ?? throw new InvalidOperationException("RECOVERY_JOB_NOT_FOUND");
        var lease = await _leases.LoadLeaseAsync(jobId, cancellationToken);
        var receipt = await FindReceiptAsync(jobId, cancellationToken);
        RecoveryReconciliationState state;
        string reason;
        if (receipt is not null && job.State is DurableJobState.Completed or DurableJobState.Failed or DurableJobState.Canceled)
        { state = RecoveryReconciliationState.ReceiptAlreadyCommitted; reason = "Terminal job and execution receipt agree that execution already produced a durable outcome."; }
        else if (lease is not null && lease.ExpiresAt > DateTimeOffset.UtcNow)
        { state = RecoveryReconciliationState.LeaseActive; reason = "A non-expired execution lease remains active; reclamation is not permitted."; }
        else if (lease is not null && lease.ExpiresAt <= DateTimeOffset.UtcNow)
        { state = RecoveryReconciliationState.StaleWorker; reason = "The recorded worker lease has expired; checkpoint and fingerprint validation are required before reacquisition."; }
        else if (job.State == DurableJobState.RecoveryRequired)
        { state = RecoveryReconciliationState.RecoveryRequired; reason = "The durable job is already marked RecoveryRequired."; }
        else
        { state = RecoveryReconciliationState.NoAction; reason = "No stale lease or receipt/state conflict was detected."; }

        var receiptFp = receipt?.ReceiptFingerprint;
        var auditFp = ResearchFingerprint.Sha256($"{batchId}|{contextId}|{jobId}|{workerFingerprint}|{job.State}|{state}|{reason}|{receiptFp}");
        await _audits.AppendRecoveryAuditAsync(new RecoveryAuditRecord(auditFp, batchId, contextId, jobId, workerFingerprint, job.State.ToString(), state.ToString(), reason, receiptFp, DateTimeOffset.UtcNow), cancellationToken);
        if (_evidence is not null)
        {
            var evidenceHash = ResearchFingerprint.Sha256($"{auditFp}|{reason}|{receiptFp}");
            await _evidence.AppendEvidenceAsync($"recovery-audit:{auditFp}", evidenceHash, cancellationToken);
            await _evidence.AppendEventAsync($"event:recovery-audit:{auditFp}", jobId, "RECOVERY_RECONCILIATION", evidenceHash, cancellationToken);
        }
        return new(batchId, contextId, jobId, state, reason, receiptFp, auditFp);
    }

    private async Task<ResourceReceiptRecord?> FindReceiptAsync(string jobId, CancellationToken cancellationToken)
    {
        var receipts = await _receipts.LoadExecutionReceiptsForJobAsync(jobId, cancellationToken);
        return receipts.Count == 0 ? null : receipts[^1];
    }
}
