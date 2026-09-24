using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

/// <summary>
/// Binds a previously validated recovery checkpoint to deterministic, caller-supplied continuation steps.
/// The service never invents research instructions: the caller owns the domain operation for each cursor.
/// </summary>
public sealed class CheckpointBoundContinuationService
{
    private readonly IResearchJobStore _jobs;
    private readonly IJobLeaseStore _leases;
    private readonly IResourceReceiptStore _receipts;
    private readonly IRecoveryAuditStore _audits;
    private readonly ILocalEvidenceStore? _evidence;
    private readonly IRecoveryPreflightStore? _preflightStore;
    private readonly IRecoveryContinuationReplayStore? _replayStore;

    public CheckpointBoundContinuationService(IResearchJobStore jobs, ILeaseInspectionStore leaseInspection, IJobLeaseStore leases, IResourceReceiptStore receipts, IRecoveryAuditStore audits, ILocalEvidenceStore? evidence = null, IRecoveryPreflightStore? preflightStore = null, IRecoveryContinuationReplayStore? replayStore = null)
    {
        _jobs = jobs;
        _leases = leases;
        _receipts = receipts;
        _audits = audits;
        _evidence = evidence;
        _preflightStore = preflightStore;
        _replayStore = replayStore;
        _leaseInspection = leaseInspection;
    }

    private readonly ILeaseInspectionStore _leaseInspection;

    public async Task<CheckpointContinuationOutcome> ExecuteAsync(
        RecoveryContinuationRequest request,
        Func<CheckpointContinuationCursor, CancellationToken, Task<CheckpointContinuationStep>> executeStepAsync,
        int maxSteps = 100_000,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executeStepAsync);
        if (maxSteps < 1) throw new ArgumentOutOfRangeException(nameof(maxSteps));

        if (_evidence is null || _audits is not IRecoveryBindingReconciliationStore ||
            _audits is not IRecoveryBindingReconciliationVerificationStore)
            throw new InvalidOperationException("RECOVERY_CONTINUATION_REQUIRES_ATOMIC_BINDING_RECONCILIATION_AND_READBACK_STORE");

        if (_preflightStore is null)
            throw new InvalidOperationException("RECOVERY_CONTINUATION_REQUIRES_RECOVERY_PREFLIGHT_STORE");
        if (_replayStore is null)
            throw new InvalidOperationException("RECOVERY_CONTINUATION_REQUIRES_REPLAY_LEDGER_STORE");
        if (_replayStore is not IRecoveryContinuationAtomicCommitStore atomicCommitStore || _replayStore is not IRecoveryContinuationReconciliationStore reconciliationStore)
            throw new InvalidOperationException("RECOVERY_CONTINUATION_REQUIRES_ATOMIC_RESULT_CHECKPOINT_STORE");

        var prepareService = new RecoveryContinuationService(_jobs, _leaseInspection, _leases, _receipts, _audits, _evidence, _preflightStore);
        var bindingService = new RecoveryPreflightBindingService(_preflightStore, _jobs, _leaseInspection);
        var reconciliationService = new RecoveryBindingReconciliationService(bindingService, _audits, _evidence);
        var recovery = await new RecoveryContinuationGateService(
                prepareService,
                reconciliationService,
                (IRecoveryBindingReconciliationVerificationStore)_audits,
                _jobs,
                _leases)
            .PrepareAndAuthorizeAsync(request, cancellationToken);

        if (recovery.State != RecoveryContinuationState.Ready)
            return CheckpointContinuationOutcome.NotStarted(recovery);

        var checkpoint = await _jobs.LoadLatestCheckpointAsync(request.JobId, cancellationToken)
            ?? throw new InvalidOperationException("RECOVERY_CHECKPOINT_DISAPPEARED_AFTER_VALIDATION");

        // Keep the execution delegate behind a separate fail-closed authorization seam.
        // The loop below is entered only after RecoveryContinuationGateService has
        // established durable reconciliation and verified its read-back.
        var invalidationGuard = new RecoveryContinuationInvalidationGuard(_jobs, _leaseInspection);
        var steps = 0;
        var current = new CheckpointContinuationCursor(checkpoint.Sequence, checkpoint.Cursor, checkpoint.StateHash);
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var beforeStep = await invalidationGuard.ValidateBeforeStepAsync(request, recovery, current, cancellationToken);
                if (!beforeStep.Authorized)
                    throw new InvalidOperationException(beforeStep.FailureCode ?? "RECOVERY_CONTINUATION_INVALIDATED_BEFORE_STEP");
                if (++steps > maxSteps) throw new InvalidOperationException("RECOVERY_CONTINUATION_STEP_LIMIT_EXCEEDED");
                if (!await _leases.RenewAsync(request.JobId, request.Worker.DeviceId, request.LeaseDuration, cancellationToken))
                    throw new InvalidOperationException("RECOVERY_LEASE_RENEWAL_FAILED");

                var operationFingerprint = RecoveryContinuationReplayFingerprint.For(request, current);
                var operation = await _replayStore.LoadAsync(operationFingerprint, cancellationToken);
                CheckpointContinuationStep step;
                if (operation is not null)
                {
                    if (operation.Sequence != current.Sequence || operation.Cursor != current.Cursor || !string.Equals(operation.StateHash, current.StateHash, StringComparison.Ordinal))
                        throw new InvalidOperationException("RECOVERY_CONTINUATION_REPLAY_CONTEXT_CONFLICT");
                    if (operation.State == RecoveryContinuationOperationState.Prepared)
                    {
                        var evidenceFingerprint = ResearchFingerprint.Sha256($"QF-RECOVERY-PREPARED-AMBIGUOUS|{operation.OperationFingerprint}|{operation.JobId}|{operation.Sequence}|{operation.Cursor}|{operation.StateHash}");
                        var existingDecision = await reconciliationStore.LoadAsync(operation.OperationFingerprint, cancellationToken);
                        if (existingDecision is null)
                        {
                            var decision = RecoveryContinuationReconciliationPolicy.CreateReviewRecord(operation, evidenceFingerprint, "Prepared-only operation has no durable result; callback replay is prohibited.", DateTimeOffset.UtcNow);
                            await reconciliationStore.RecordAmbiguousAsync(decision, cancellationToken);
                        }
                        throw new InvalidOperationException(RecoveryContinuationReconciliationPolicy.AmbiguousPreparedCode);
                    }
                    step = new CheckpointContinuationStep(operation.NextCursor!.Value, operation.ResultStateHash!, operation.Completed!.Value, operation.ResultFingerprint);
                }
                else
                {
                    await _replayStore.PrepareAsync(new RecoveryContinuationOperation(
                        operationFingerprint, request.JobId, current.Sequence, current.Cursor, current.StateHash,
                        RecoveryContinuationOperationState.Prepared, null, null, null, null, DateTimeOffset.UtcNow, null), cancellationToken);
                    CheckpointContinuationStep? authorizedStep = null;
                    await RecoveryContinuationExecutionGate.ExecuteAsync(
                        recovery,
                        async token => authorizedStep = await executeStepAsync(current, token),
                        cancellationToken);
                    step = authorizedStep ?? throw new InvalidOperationException("RECOVERY_CONTINUATION_STEP_NOT_EXECUTED");
                }
                var beforeCommit = await invalidationGuard.ValidateBeforeCommitAsync(request, recovery, current, cancellationToken);
                if (!beforeCommit.Authorized)
                    throw new InvalidOperationException(beforeCommit.FailureCode ?? "RECOVERY_CONTINUATION_INVALIDATED_BEFORE_COMMIT");
                if (step.NextCursor < current.Cursor)
                    throw new InvalidOperationException("RECOVERY_CURSOR_REGRESSION");
                if (string.IsNullOrWhiteSpace(step.StateHash))
                    throw new InvalidOperationException("RECOVERY_STEP_STATE_HASH_REQUIRED");

                var nextSequence = checked(current.Sequence + 1);
                var checkpointToSave = new ResearchCheckpoint(
                    request.JobId,
                    nextSequence,
                    step.NextCursor,
                    request.ExpectedDatasetFingerprint,
                    request.ExpectedConfigurationFingerprint,
                    request.ExpectedEngineFingerprint,
                    step.StateHash,
                    DateTimeOffset.UtcNow);
                await atomicCommitStore.CommitResultAndCheckpointAsync(
                    operationFingerprint,
                    step.NextCursor,
                    step.StateHash,
                    step.Completed,
                    step.ResultFingerprint,
                    checkpointToSave,
                    cancellationToken);
                current = new CheckpointContinuationCursor(nextSequence, step.NextCursor, step.StateHash);

                if (!step.Completed) continue;

                var job = await _jobs.LoadJobAsync(request.JobId, cancellationToken)
                    ?? throw new InvalidOperationException("RECOVERY_JOB_NOT_FOUND_AFTER_CONTINUATION");
                if (job.State != DurableJobState.Running)
                    throw new InvalidOperationException($"RECOVERY_JOB_STATE_CHANGED_TO_{job.State.ToString().ToUpperInvariant()}");
                var completed = ResearchJobLifecycle.Complete(job, DateTimeOffset.UtcNow);
                await _jobs.SaveJobAsync(completed, cancellationToken);

                var resultFingerprint = ResearchFingerprint.Sha256($"{request.BatchId}|{request.ContextId}|{request.JobId}|{request.Worker.IdentityFingerprint}|{current.Sequence}|{current.Cursor}|{current.StateHash}|{step.ResultFingerprint}");
                if (_evidence is not null)
                {
                    await _evidence.AppendEvidenceAsync($"recovery-continuation-result:{resultFingerprint}", resultFingerprint, cancellationToken);
                    await _evidence.AppendEventAsync($"event:recovery-continuation-result:{resultFingerprint}", request.JobId, "RECOVERY_CONTINUATION_COMPLETED", resultFingerprint, cancellationToken);
                }
                return new CheckpointContinuationOutcome(true, recovery, current.Sequence, current.Cursor, current.StateHash, resultFingerprint, null);
            }
        }
        catch (OperationCanceledException)
        {
            var job = await _jobs.LoadJobAsync(request.JobId, CancellationToken.None);
            if (job is not null && job.State is not DurableJobState.Completed and not DurableJobState.Canceled)
                await _jobs.SaveJobAsync(ResearchJobLifecycle.Cancel(job, DateTimeOffset.UtcNow), CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            var job = await _jobs.LoadJobAsync(request.JobId, CancellationToken.None);
            if (job is not null && job.State is not DurableJobState.Completed and not DurableJobState.Canceled)
                await _jobs.SaveJobAsync(ResearchJobLifecycle.Fail(job, ex.Message, DateTimeOffset.UtcNow), CancellationToken.None);
            throw;
        }
        finally
        {
            await _leases.ReleaseAsync(request.JobId, request.Worker.DeviceId, CancellationToken.None);
        }
    }
}

public sealed record CheckpointContinuationCursor(long Sequence, int Cursor, string StateHash);

public sealed record CheckpointContinuationStep(int NextCursor, string StateHash, bool Completed, string? ResultFingerprint = null);

public sealed record CheckpointContinuationOutcome(
    bool Started,
    RecoveryContinuationResult Recovery,
    long? FinalSequence,
    int? FinalCursor,
    string? FinalStateHash,
    string? ResultFingerprint,
    string? FailureCode)
{
    public static CheckpointContinuationOutcome NotStarted(RecoveryContinuationResult recovery) =>
        new(false, recovery, recovery.CheckpointSequence, recovery.CheckpointCursor, recovery.CheckpointStateHash, null, recovery.FailureCode);
}
