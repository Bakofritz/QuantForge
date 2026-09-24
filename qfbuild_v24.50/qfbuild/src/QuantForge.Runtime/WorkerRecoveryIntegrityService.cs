using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public enum WorkerRecoveryIntegrityState
{
    SafeToRecover,
    TerminalOutcomeAlreadyCommitted,
    TerminalOutcomeConflict,
    CancellationRequested,
    LeaseStillActive,
    RecoveryStateNotEligible
}

public sealed record WorkerRecoveryIntegrityResult(
    string JobId,
    WorkerRecoveryIntegrityState State,
    string Reason,
    string? TerminalReceiptFingerprint,
    int TerminalReceiptCount,
    string ResultFingerprint);

/// <summary>
/// Read-only gate used before a worker attempts recovery. It reconciles job state,
/// terminal receipts, cancellation, and lease ownership without changing durable state.
/// </summary>
public sealed class WorkerRecoveryIntegrityService
{
    private readonly IResearchJobStore _jobs;
    private readonly ILeaseInspectionStore _leases;
    private readonly IResourceReceiptStore _receipts;

    public WorkerRecoveryIntegrityService(IResearchJobStore jobs, ILeaseInspectionStore leases, IResourceReceiptStore receipts)
    {
        _jobs = jobs;
        _leases = leases;
        _receipts = receipts;
    }

    public async Task<WorkerRecoveryIntegrityResult> ValidateAsync(
        string jobId,
        string expectedWorkerDeviceId,
        CancellationToken cancellationToken = default)
    {
        var job = await _jobs.LoadJobAsync(jobId, cancellationToken)
            ?? throw new InvalidOperationException("RECOVERY_JOB_NOT_FOUND");
        var receipts = await _receipts.LoadExecutionReceiptsForJobAsync(jobId, cancellationToken);
        var terminal = receipts.Where(IsTerminal).ToList();
        var cancellation = await _jobs.LoadCancellationAsync(jobId, cancellationToken);
        var lease = await _leases.LoadLeaseAsync(jobId, cancellationToken);

        WorkerRecoveryIntegrityState state;
        string reason;
        string? receiptFingerprint = terminal.Count == 1 ? terminal[0].ReceiptFingerprint : null;

        if (terminal.Count > 1)
        {
            state = WorkerRecoveryIntegrityState.TerminalOutcomeConflict;
            reason = "Multiple terminal receipts exist for one job; recovery is blocked until the durable history is reconciled.";
        }
        else if (terminal.Count == 1 && job.State is DurableJobState.Completed or DurableJobState.Failed or DurableJobState.Canceled)
        {
            state = WorkerRecoveryIntegrityState.TerminalOutcomeAlreadyCommitted;
            reason = "A terminal job state and terminal receipt already exist; recovery is not permitted.";
        }
        else if (terminal.Count == 1)
        {
            state = WorkerRecoveryIntegrityState.TerminalOutcomeConflict;
            reason = "A terminal receipt exists while the durable job is not terminal; recovery is blocked by a state mismatch.";
        }
        else if (cancellation is not null)
        {
            state = WorkerRecoveryIntegrityState.CancellationRequested;
            reason = "A durable cancellation request exists; recovery is prohibited.";
        }
        else if (lease is not null && lease.ExpiresAt > DateTimeOffset.UtcNow &&
                 !string.Equals(lease.DeviceId, expectedWorkerDeviceId, StringComparison.Ordinal))
        {
            state = WorkerRecoveryIntegrityState.LeaseStillActive;
            reason = "Another worker owns a non-expired lease; recovery is prohibited.";
        }
        else if (job.State is DurableJobState.Completed or DurableJobState.Failed or DurableJobState.Canceled)
        {
            state = WorkerRecoveryIntegrityState.TerminalOutcomeConflict;
            reason = "The job is terminal but no terminal receipt exists; recovery cannot safely invent the missing receipt.";
        }
        else if (job.State is not (DurableJobState.Running or DurableJobState.Paused or DurableJobState.RecoveryRequired))
        {
            state = WorkerRecoveryIntegrityState.RecoveryStateNotEligible;
            reason = $"Job state {job.State} is not eligible for controlled recovery.";
        }
        else
        {
            state = WorkerRecoveryIntegrityState.SafeToRecover;
            reason = "No terminal outcome, cancellation, active foreign lease, or state mismatch was found.";
        }

        var fingerprint = ResearchFingerprint.Sha256(
            $"{jobId}|{job.State}|{state}|{reason}|{receiptFingerprint}|{terminal.Count}");
        return new WorkerRecoveryIntegrityResult(jobId, state, reason, receiptFingerprint, terminal.Count, fingerprint);
    }

    private static bool IsTerminal(ResourceReceiptRecord receipt) =>
        receipt.State is "Completed" or "Failed" or "Canceled";
}
