using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public enum RecoveryBindingReconciliationState
{
    BoundAndConsistent,
    BlockedByBindingMismatch
}

public sealed record RecoveryBindingReconciliationResult(
    string BatchId,
    string ContextId,
    string JobId,
    string WorkerDeviceId,
    RecoveryBindingReconciliationState State,
    bool RecoveryMayContinue,
    string Reason,
    string PreflightFingerprint,
    string? CheckpointFingerprint,
    string? LeaseFingerprint,
    string AuditFingerprint,
    string EvidenceFingerprint);

/// <summary>
/// Converts recovery preflight binding validation into a durable reconciliation/audit record.
/// This service is read-only with respect to recovery state: it never repairs or advances a job.
/// </summary>
public sealed class RecoveryBindingReconciliationService
{
    private readonly RecoveryPreflightBindingService _binding;
    private readonly IRecoveryAuditStore _audits;
    private readonly ILocalEvidenceStore? _evidence;

    public RecoveryBindingReconciliationService(
        RecoveryPreflightBindingService binding,
        IRecoveryAuditStore audits,
        ILocalEvidenceStore? evidence = null)
    {
        _binding = binding;
        _audits = audits;
        _evidence = evidence;
    }

    public async Task<RecoveryBindingReconciliationResult> ReconcileAsync(
        RecoveryContinuationRequest request,
        CancellationToken cancellationToken = default)
    {
        var binding = await _binding.ValidateAsync(request, cancellationToken);
        var state = binding.Passed
            ? RecoveryBindingReconciliationState.BoundAndConsistent
            : RecoveryBindingReconciliationState.BlockedByBindingMismatch;
        var auditFingerprint = ResearchFingerprint.Sha256(
            $"{request.BatchId}|{request.ContextId}|{request.JobId}|{request.Worker.DeviceId}|{state}|{binding.ResultFingerprint}");
        var evidenceFingerprint = ResearchFingerprint.Sha256(
            $"{auditFingerprint}|{binding.PreflightFingerprint}|{binding.CheckpointFingerprint}|{binding.LeaseFingerprint}|{binding.Reason}");

        var audit = new RecoveryAuditRecord(
            auditFingerprint,
            request.BatchId,
            request.ContextId,
            request.JobId,
            request.Worker.IdentityFingerprint,
            "RecoveryPreflightBinding",
            state.ToString(),
            binding.Reason,
            null,
            DateTimeOffset.UtcNow);
        var evidenceId = $"recovery-binding-reconciliation:{evidenceFingerprint}";
        var eventId = $"event:recovery-binding-reconciliation:{auditFingerprint}";

        if (_evidence is not null && _audits is IRecoveryBindingReconciliationStore atomicStore)
        {
            await atomicStore.AppendRecoveryBindingReconciliationAtomicallyAsync(
                audit, evidenceId, evidenceFingerprint, eventId, request.JobId,
                "RECOVERY_BINDING_RECONCILIATION", evidenceFingerprint, cancellationToken);
        }
        else
        {
            await _audits.AppendRecoveryAuditAsync(audit, cancellationToken);
            if (_evidence is not null)
            {
                await _evidence.AppendEvidenceAsync(evidenceId, evidenceFingerprint, cancellationToken);
                await _evidence.AppendEventAsync(eventId, request.JobId, "RECOVERY_BINDING_RECONCILIATION", evidenceFingerprint, cancellationToken);
            }
        }

        return new(
            request.BatchId,
            request.ContextId,
            request.JobId,
            request.Worker.DeviceId,
            state,
            binding.Passed,
            binding.Reason,
            binding.PreflightFingerprint,
            binding.CheckpointFingerprint,
            binding.LeaseFingerprint,
            auditFingerprint,
            evidenceFingerprint);
    }
}
