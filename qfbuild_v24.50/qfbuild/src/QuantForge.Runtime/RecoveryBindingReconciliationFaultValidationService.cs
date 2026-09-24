using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed class RecoveryBindingReconciliationFaultValidationService
{
    private readonly ILocalEvidenceStore? _evidence;
    public RecoveryBindingReconciliationFaultValidationService(ILocalEvidenceStore? evidence = null) => _evidence = evidence;

    public async Task<RecoveryBindingReconciliationFaultValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var result = RecoveryBindingReconciliationFaultHarness.Run(cancellationToken);
        if (_evidence is not null)
        {
            await _evidence.AppendEvidenceAsync($"recovery-binding-reconciliation-fault:{result.ResultFingerprint}", result.ResultFingerprint, cancellationToken);
            await _evidence.AppendEventAsync($"event:recovery-binding-reconciliation-fault:{result.ResultFingerprint}", "recovery-binding-reconciliation-fault", "RECOVERY_BINDING_RECONCILIATION_FAULT_VALIDATION", result.ResultFingerprint, cancellationToken);
        }
        return result;
    }
}
