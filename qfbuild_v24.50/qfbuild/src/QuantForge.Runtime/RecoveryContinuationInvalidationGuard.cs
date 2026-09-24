using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

/// <summary>
/// Revalidates the recovery authority immediately before a continuation step and immediately
/// before its resulting checkpoint is committed. This closes the post-authorization invalidation
/// window for durable cancellation, lease loss, job-state mutation, and checkpoint replacement.
/// </summary>
public sealed class RecoveryContinuationInvalidationGuard
{
    private readonly IResearchJobStore _jobs;
    private readonly ILeaseInspectionStore _leases;

    public RecoveryContinuationInvalidationGuard(IResearchJobStore jobs, ILeaseInspectionStore leases)
    {
        _jobs = jobs;
        _leases = leases;
    }

    public Task<RecoveryContinuationAuthorityCheck> ValidateBeforeStepAsync(
        RecoveryContinuationRequest request,
        RecoveryContinuationResult authorization,
        CheckpointContinuationCursor expectedCheckpoint,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(request, authorization, expectedCheckpoint, "BEFORE_STEP", cancellationToken);

    public Task<RecoveryContinuationAuthorityCheck> ValidateBeforeCommitAsync(
        RecoveryContinuationRequest request,
        RecoveryContinuationResult authorization,
        CheckpointContinuationCursor expectedCheckpoint,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(request, authorization, expectedCheckpoint, "BEFORE_COMMIT", cancellationToken);

    private async Task<RecoveryContinuationAuthorityCheck> ValidateAsync(
        RecoveryContinuationRequest request,
        RecoveryContinuationResult authorization,
        CheckpointContinuationCursor expectedCheckpoint,
        string phase,
        CancellationToken cancellationToken)
    {
        if (authorization.State != RecoveryContinuationState.Ready)
            return Block(phase, "RECOVERY_CONTINUATION_NOT_AUTHORIZED", "Authorization is no longer Ready.");

        var job = await _jobs.LoadJobAsync(request.JobId, cancellationToken);
        if (job is null)
            return Block(phase, "RECOVERY_JOB_NOT_FOUND_AFTER_AUTHORIZATION", "The durable job disappeared after authorization.");
        if (job.State != DurableJobState.Running)
            return Block(phase, $"RECOVERY_JOB_STATE_CHANGED_TO_{job.State.ToString().ToUpperInvariant()}", "The durable job is no longer Running.");

        if (await _jobs.LoadCancellationAsync(request.JobId, cancellationToken) is not null)
            return Block(phase, "RECOVERY_CANCELLATION_REQUESTED_AFTER_AUTHORIZATION", "A durable cancellation request appeared after authorization.");

        var lease = await _leases.LoadLeaseAsync(request.JobId, cancellationToken);
        if (lease is null)
            return Block(phase, "RECOVERY_LEASE_DISAPPEARED_AFTER_AUTHORIZATION", "The recovery lease disappeared after authorization.");
        if (!string.Equals(lease.DeviceId, request.Worker.DeviceId, StringComparison.Ordinal))
            return Block(phase, "RECOVERY_LEASE_OWNER_CHANGED_AFTER_AUTHORIZATION", "The recovery lease owner changed after authorization.");
        if (lease.ExpiresAt <= DateTimeOffset.UtcNow)
            return Block(phase, "RECOVERY_LEASE_EXPIRED_AFTER_AUTHORIZATION", "The recovery lease expired after authorization.");

        var checkpoint = await _jobs.LoadLatestCheckpointAsync(request.JobId, cancellationToken);
        if (checkpoint is null)
            return Block(phase, "RECOVERY_CHECKPOINT_DISAPPEARED_AFTER_AUTHORIZATION", "The authorized checkpoint disappeared.");

        if (checkpoint.Sequence != expectedCheckpoint.Sequence ||
            checkpoint.Cursor != expectedCheckpoint.Cursor ||
            !string.Equals(checkpoint.StateHash, expectedCheckpoint.StateHash, StringComparison.Ordinal))
            return Block(phase, "RECOVERY_CHECKPOINT_CHANGED_AFTER_AUTHORIZATION", "The durable checkpoint no longer matches the authorized continuation cursor.");

        return new(true, phase, null, "Recovery authority remained valid at the requested execution boundary.");
    }

    private static RecoveryContinuationAuthorityCheck Block(string phase, string code, string detail) =>
        new(false, phase, code, detail);
}

public sealed record RecoveryContinuationAuthorityCheck(
    bool Authorized,
    string Phase,
    string? FailureCode,
    string Detail);
