using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

/// <summary>
/// Final fail-closed authorization boundary between recovery preparation and execution.
/// Recovery may continue only after the binding reconciliation is durably committed and
/// read back as complete. This service never grants live-trading authority.
/// </summary>
public sealed class RecoveryContinuationGateService
{
    private readonly RecoveryContinuationService _prepare;
    private readonly RecoveryBindingReconciliationService _reconciliation;
    private readonly IRecoveryBindingReconciliationVerificationStore _verification;
    private readonly IResearchJobStore _jobs;
    private readonly IJobLeaseStore _leases;

    public RecoveryContinuationGateService(
        RecoveryContinuationService prepare,
        RecoveryBindingReconciliationService reconciliation,
        IRecoveryBindingReconciliationVerificationStore verification,
        IResearchJobStore jobs,
        IJobLeaseStore leases)
    {
        _prepare = prepare;
        _reconciliation = reconciliation;
        _verification = verification;
        _jobs = jobs;
        _leases = leases;
    }

    public async Task<RecoveryContinuationResult> PrepareAndAuthorizeAsync(
        RecoveryContinuationRequest request,
        CancellationToken cancellationToken = default)
    {
        var prepared = await _prepare.PrepareAsync(request, cancellationToken);
        if (prepared.State != RecoveryContinuationState.Ready)
            return prepared;

        try
        {
            var reconciliation = await _reconciliation.ReconcileAsync(request, cancellationToken);
            var evidenceId = $"recovery-binding-reconciliation:{reconciliation.EvidenceFingerprint}";
            var eventId = $"event:recovery-binding-reconciliation:{reconciliation.AuditFingerprint}";
            var durable = await _verification.VerifyRecoveryBindingReconciliationAsync(
                reconciliation.AuditFingerprint,
                evidenceId,
                eventId,
                reconciliation.EvidenceFingerprint,
                cancellationToken);

            if (reconciliation.RecoveryMayContinue && durable)
                return prepared;

            return await BlockAsync(request, prepared, reconciliation.Reason,
                reconciliation.RecoveryMayContinue ? "RECOVERY_RECONCILIATION_READBACK_FAILED" : "RECOVERY_BINDING_RECONCILIATION_BLOCKED",
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return await BlockAsync(request, prepared,
                "Recovery continuation was blocked because reconciliation could not be durably established: " + ex.Message,
                "RECOVERY_RECONCILIATION_NOT_DURABLE", cancellationToken);
        }
    }

    private async Task<RecoveryContinuationResult> BlockAsync(
        RecoveryContinuationRequest request,
        RecoveryContinuationResult prepared,
        string reason,
        string failureCode,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.LoadJobAsync(request.JobId, cancellationToken);
        if (job is not null && job.State == DurableJobState.Running)
        {
            var recoveryRequired = ResearchJobLifecycle.Recover(job, DateTimeOffset.UtcNow)
                with { FailureCode = failureCode };
            await _jobs.SaveJobAsync(recoveryRequired, cancellationToken);
        }

        await _leases.ReleaseAsync(request.JobId, request.Worker.DeviceId, CancellationToken.None);
        var audit = ResearchFingerprint.Sha256($"{prepared.AuditFingerprint}|{failureCode}|{reason}");
        return prepared with
        {
            State = RecoveryContinuationState.ReconciliationBlocked,
            AuditFingerprint = audit,
            FailureCode = failureCode
        };
    }
}
