using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed class RecoveryPreflightBindingFaultValidationService
{
    private readonly ILocalEvidenceStore? _evidence;

    public RecoveryPreflightBindingFaultValidationService(ILocalEvidenceStore? evidence = null)
    {
        _evidence = evidence;
    }

    public async Task<RecoveryBindingFaultValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = RecoveryPreflightBindingFaultHarness.Run();
        if (_evidence is not null)
        {
            await _evidence.AppendEvidenceAsync($"recovery-binding-fault-harness:{result.ResultFingerprint}", result.ResultFingerprint, cancellationToken);
            await _evidence.AppendEventAsync($"event:recovery-binding-fault-harness:{result.ResultFingerprint}", "recovery-binding-fault-harness", "RECOVERY_BINDING_FAULT_VALIDATION", result.ResultFingerprint, cancellationToken);
        }
        return result;
    }
}
