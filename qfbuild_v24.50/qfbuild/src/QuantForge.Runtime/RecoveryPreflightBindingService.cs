using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record RecoveryPreflightBindingResult(
    bool Passed,
    string Reason,
    string PreflightFingerprint,
    string? CheckpointFingerprint,
    string? LeaseFingerprint,
    string ResultFingerprint);

/// <summary>
/// Reconciles the durable recovery preflight decision with the checkpoint and lease actually used.
/// This is read-only: it never changes recovery state or repairs mismatches.
/// </summary>
public sealed class RecoveryPreflightBindingService
{
    private readonly IRecoveryPreflightStore _preflights;
    private readonly IResearchJobStore _jobs;
    private readonly ILeaseInspectionStore _leases;

    public RecoveryPreflightBindingService(
        IRecoveryPreflightStore preflights,
        IResearchJobStore jobs,
        ILeaseInspectionStore leases)
    {
        _preflights = preflights;
        _jobs = jobs;
        _leases = leases;
    }

    public async Task<RecoveryPreflightBindingResult> ValidateAsync(
        RecoveryContinuationRequest request,
        CancellationToken cancellationToken = default)
    {
        var preflights = await _preflights.LoadRecoveryPreflightsForJobAsync(request.JobId, cancellationToken);
        var accepted = preflights
            .Where(p => string.Equals(p.Decision, "Allowed", StringComparison.Ordinal)
                     && string.Equals(p.WorkerDeviceId, request.Worker.DeviceId, StringComparison.Ordinal))
            .OrderByDescending(p => p.RecordedAt)
            .FirstOrDefault();

        if (accepted is null)
            return Fail("RECOVERY_PREFLIGHT_NOT_FOUND", "No allowed recovery preflight exists for this worker and job.", "none", null, null);

        var checkpoint = await _jobs.LoadLatestCheckpointAsync(request.JobId, cancellationToken);
        if (checkpoint is null)
            return Fail(accepted.PreflightFingerprint, "RECOVERY_CHECKPOINT_MISSING", "The checkpoint bound to recovery no longer exists.", null, null);

        var checkpointFingerprint = ResearchFingerprint.Sha256(
            $"{checkpoint.JobId}|{checkpoint.Sequence}|{checkpoint.Cursor}|{checkpoint.StateHash}|{checkpoint.DatasetFingerprint}|{checkpoint.ConfigurationFingerprint}|{checkpoint.EngineFingerprint}");

        if (accepted.CheckpointSequence != checkpoint.Sequence ||
            accepted.CheckpointCursor != checkpoint.Cursor ||
            !string.Equals(accepted.CheckpointStateHash, checkpoint.StateHash, StringComparison.Ordinal))
            return Fail(accepted.PreflightFingerprint, "RECOVERY_CHECKPOINT_BINDING_MISMATCH", "The current checkpoint differs from the checkpoint approved during recovery preflight.", checkpointFingerprint, null);

        var lease = await _leases.LoadLeaseAsync(request.JobId, cancellationToken);
        if (lease is null)
            return Fail(accepted.PreflightFingerprint, "RECOVERY_LEASE_MISSING", "The lease bound to recovery no longer exists.", checkpointFingerprint, null);

        var leaseFingerprint = ResearchFingerprint.Sha256(
            $"{lease.JobId}|{lease.DeviceId}|{lease.Version}|{lease.AcquiredAt:O}|{lease.ExpiresAt:O}");

        if (accepted.AcceptedLeaseVersion != lease.Version ||
            !string.Equals(accepted.AcceptedLeaseFingerprint, leaseFingerprint, StringComparison.Ordinal) ||
            !string.Equals(lease.DeviceId, request.Worker.DeviceId, StringComparison.Ordinal) ||
            lease.ExpiresAt <= DateTimeOffset.UtcNow)
            return Fail(accepted.PreflightFingerprint, "RECOVERY_LEASE_BINDING_MISMATCH", "The current lease differs from, or is no longer valid for, the lease approved during recovery preflight.", checkpointFingerprint, leaseFingerprint);

        var resultFingerprint = ResearchFingerprint.Sha256(
            $"{accepted.PreflightFingerprint}|{checkpointFingerprint}|{leaseFingerprint}|BOUND");
        return new RecoveryPreflightBindingResult(true, "Preflight, checkpoint, and active lease agree.", accepted.PreflightFingerprint, checkpointFingerprint, leaseFingerprint, resultFingerprint);
    }

    private static RecoveryPreflightBindingResult Fail(string preflightFingerprint, string code, string reason, string? checkpointFingerprint, string? leaseFingerprint)
    {
        var resultFingerprint = ResearchFingerprint.Sha256($"{preflightFingerprint}|{code}|{checkpointFingerprint}|{leaseFingerprint}");
        return new RecoveryPreflightBindingResult(false, $"{code}: {reason}", preflightFingerprint, checkpointFingerprint, leaseFingerprint, resultFingerprint);
    }
}
