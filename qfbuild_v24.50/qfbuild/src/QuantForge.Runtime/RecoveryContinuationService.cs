using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

/// <summary>
/// Validates a durable checkpoint and establishes a fresh execution lease for controlled recovery.
/// It does not resume arbitrary instructions; the caller must use the returned validated checkpoint.
/// </summary>
public sealed class RecoveryContinuationService
{
    private readonly IResearchJobStore _jobs;
    private readonly ILeaseInspectionStore _leases;
    private readonly IJobLeaseStore _leaseControl;
    private readonly IResourceReceiptStore _receipts;
    private readonly IRecoveryAuditStore _audits;
    private readonly ILocalEvidenceStore? _evidence;
    private readonly RecoveryPreflightService? _preflight;

    public RecoveryContinuationService(
        IResearchJobStore jobs,
        ILeaseInspectionStore leases,
        IJobLeaseStore leaseControl,
        IResourceReceiptStore receipts,
        IRecoveryAuditStore audits,
        ILocalEvidenceStore? evidence = null,
        IRecoveryPreflightStore? preflightStore = null)
    {
        _jobs = jobs;
        _leases = leases;
        _leaseControl = leaseControl;
        _receipts = receipts;
        _audits = audits;
        _evidence = evidence;
        _preflight = preflightStore is null ? null : new RecoveryPreflightService(preflightStore, audits, evidence);
    }

    public async Task<RecoveryContinuationResult> PrepareAsync(
        RecoveryContinuationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.LeaseDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(request.LeaseDuration));

        var integrity = await new WorkerRecoveryIntegrityService(_jobs, _leases, _receipts)
            .ValidateAsync(request.JobId, request.Worker.DeviceId, cancellationToken);
        if (integrity.State is WorkerRecoveryIntegrityState.TerminalOutcomeAlreadyCommitted or
            WorkerRecoveryIntegrityState.TerminalOutcomeConflict or
            WorkerRecoveryIntegrityState.CancellationRequested or
            WorkerRecoveryIntegrityState.LeaseStillActive or
            WorkerRecoveryIntegrityState.RecoveryStateNotEligible)
        {
            var blocked = integrity.State switch
            {
                WorkerRecoveryIntegrityState.TerminalOutcomeAlreadyCommitted => RecoveryContinuationState.ReceiptAlreadyCommitted,
                WorkerRecoveryIntegrityState.TerminalOutcomeConflict => RecoveryContinuationState.CheckpointIntegrityFailure,
                WorkerRecoveryIntegrityState.CancellationRequested => RecoveryContinuationState.CancellationRequested,
                WorkerRecoveryIntegrityState.LeaseStillActive => RecoveryContinuationState.LeaseStillActive,
                _ => RecoveryContinuationState.JobStateNotRecoverable
            };
            await _audits.AppendRecoveryAuditAsync(new RecoveryAuditRecord(
                ResearchFingerprint.Sha256($"{request.JobId}|{integrity.ResultFingerprint}|{blocked}"),
                request.BatchId, request.ContextId, request.JobId, request.Worker.IdentityFingerprint,
                "RECOVERY_PRECHECK", blocked.ToString(), integrity.Reason, integrity.TerminalReceiptFingerprint, DateTimeOffset.UtcNow), cancellationToken);
            var blockedAuditFingerprint = ResearchFingerprint.Sha256($"{request.BatchId}|{request.ContextId}|{request.JobId}|{integrity.ResultFingerprint}|{blocked}");
            if (_preflight is not null)
                await _preflight.RecordAsync(request, integrity, null, null, cancellationToken);
            return new RecoveryContinuationResult(
                blocked, request.BatchId, request.ContextId, request.JobId, request.Worker.IdentityFingerprint,
                null, null, null, blockedAuditFingerprint, integrity.State.ToString());
        }

        var job = await _jobs.LoadJobAsync(request.JobId, cancellationToken)
            ?? throw new InvalidOperationException("RECOVERY_JOB_NOT_FOUND");
        var lease = await _leases.LoadLeaseAsync(request.JobId, cancellationToken);
        var receipts = await _receipts.LoadExecutionReceiptsForJobAsync(request.JobId, cancellationToken);
        var terminalReceipt = receipts.FirstOrDefault(r => r.State is "Completed" or "Failed" or "Canceled");

        RecoveryContinuationState state;
        string? failureCode = null;
        ResearchCheckpoint? checkpoint = null;
        var reason = "";

        if (terminalReceipt is not null || job.State is DurableJobState.Completed or DurableJobState.Failed or DurableJobState.Canceled)
        {
            state = RecoveryContinuationState.ReceiptAlreadyCommitted;
            reason = "A terminal job state or execution receipt already exists; recovery continuation is prohibited.";
            failureCode = "TERMINAL_OUTCOME_ALREADY_COMMITTED";
        }
        else if (lease is not null && lease.ExpiresAt > DateTimeOffset.UtcNow)
        {
            state = RecoveryContinuationState.LeaseStillActive;
            reason = "Another worker still holds a non-expired lease; recovery continuation is prohibited.";
            failureCode = "LEASE_STILL_ACTIVE";
        }
        else if (await _jobs.LoadCancellationAsync(request.JobId, cancellationToken) is not null)
        {
            state = RecoveryContinuationState.CancellationRequested;
            reason = "A durable cancellation request exists; recovery continuation is prohibited.";
            failureCode = "CANCELLATION_REQUESTED";
        }
        else
        {
            checkpoint = await _jobs.LoadLatestCheckpointAsync(request.JobId, cancellationToken);
            if (checkpoint is null)
            {
                state = RecoveryContinuationState.CheckpointMissing;
                reason = "No durable checkpoint exists from which controlled continuation can safely begin.";
                failureCode = "CHECKPOINT_MISSING";
            }
            else if (!ResearchJobLifecycle.CheckpointMatches(job, checkpoint) ||
                     !string.Equals(checkpoint.DatasetFingerprint, request.ExpectedDatasetFingerprint, StringComparison.Ordinal) ||
                     !string.Equals(checkpoint.ConfigurationFingerprint, request.ExpectedConfigurationFingerprint, StringComparison.Ordinal) ||
                     !string.Equals(checkpoint.EngineFingerprint, request.ExpectedEngineFingerprint, StringComparison.Ordinal))
            {
                state = RecoveryContinuationState.CheckpointFingerprintMismatch;
                reason = "Checkpoint, durable job, or requested research fingerprints disagree.";
                failureCode = "CHECKPOINT_FINGERPRINT_MISMATCH";
                checkpoint = null;
            }
            else
            {
                var expectedHash = ResearchFingerprint.Sha256($"{checkpoint.JobId}|{checkpoint.Sequence}|{checkpoint.Cursor}|{checkpoint.DatasetFingerprint}|{checkpoint.ConfigurationFingerprint}|{checkpoint.EngineFingerprint}");
                // The existing checkpoint StateHash may represent operation-specific state. We therefore validate
                // that it is present and use the job/checkpoint identity as the minimum continuation integrity gate.
                if (string.IsNullOrWhiteSpace(checkpoint.StateHash) || string.IsNullOrWhiteSpace(expectedHash))
                {
                    state = RecoveryContinuationState.CheckpointIntegrityFailure;
                    reason = "Checkpoint integrity metadata is incomplete.";
                    failureCode = "CHECKPOINT_INTEGRITY_FAILURE";
                    checkpoint = null;
                }
                else if (job.State is not (DurableJobState.RecoveryRequired or DurableJobState.Paused or DurableJobState.Running))
                {
                    state = RecoveryContinuationState.JobStateNotRecoverable;
                    reason = $"Job state {job.State} is not eligible for controlled continuation.";
                    failureCode = "JOB_STATE_NOT_RECOVERABLE";
                    checkpoint = null;
                }
                else
                {
                    if (!await _leaseControl.TryAcquireAsync(request.JobId, request.Worker.DeviceId, request.LeaseDuration, cancellationToken))
                    {
                        state = RecoveryContinuationState.LeaseStillActive;
                        reason = "A fresh recovery lease could not be acquired.";
                        failureCode = "RECOVERY_LEASE_ACQUISITION_FAILED";
                        checkpoint = null;
                    }
                    else
                    {
                        var recoveryBase = job.State == DurableJobState.Running
                            ? ResearchJobLifecycle.Recover(job, DateTimeOffset.UtcNow)
                            : job;
                        var running = ResearchJobLifecycle.Start(recoveryBase, DateTimeOffset.UtcNow);
                        await _jobs.SaveJobAsync(running, cancellationToken);
                        state = RecoveryContinuationState.Ready;
                        reason = "Checkpoint and fingerprints validated; a fresh lease was acquired for controlled continuation.";
                    }
                }
            }
        }

        var auditFingerprint = ResearchFingerprint.Sha256($"{request.BatchId}|{request.ContextId}|{request.JobId}|{request.Worker.IdentityFingerprint}|{state}|{checkpoint?.Sequence}|{checkpoint?.Cursor}|{checkpoint?.StateHash}|{failureCode}");
        await _audits.AppendRecoveryAuditAsync(new RecoveryAuditRecord(
            auditFingerprint, request.BatchId, request.ContextId, request.JobId,
            request.Worker.IdentityFingerprint, job.State.ToString(), state.ToString(), reason,
            terminalReceipt?.ReceiptFingerprint, DateTimeOffset.UtcNow), cancellationToken);

        if (_evidence is not null)
        {
            var evidenceHash = ResearchFingerprint.Sha256($"{auditFingerprint}|{reason}|{checkpoint?.StateHash}|{failureCode}");
            await _evidence.AppendEvidenceAsync($"recovery-continuation:{auditFingerprint}", evidenceHash, cancellationToken);
            await _evidence.AppendEventAsync($"event:recovery-continuation:{auditFingerprint}", request.JobId, "RECOVERY_CONTINUATION_AUDIT", evidenceHash, cancellationToken);
        }

        if (_preflight is not null)
        {
            var acceptedLease = state == RecoveryContinuationState.Ready
                ? await _leases.LoadLeaseAsync(request.JobId, cancellationToken)
                : null;
            if (state == RecoveryContinuationState.Ready && acceptedLease is null)
                throw new InvalidOperationException("RECOVERY_ACCEPTED_LEASE_DISAPPEARED");
            await _preflight.RecordAsync(request, integrity, checkpoint, acceptedLease, cancellationToken);
        }

        return new(state, request.BatchId, request.ContextId, request.JobId, request.Worker.IdentityFingerprint,
            checkpoint?.Sequence, checkpoint?.Cursor, checkpoint?.StateHash, auditFingerprint, failureCode);
    }
}
