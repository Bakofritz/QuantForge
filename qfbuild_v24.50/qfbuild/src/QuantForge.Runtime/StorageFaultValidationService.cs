using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed class StorageFaultValidationService
{
    private readonly StorageFaultInjectionHarness _harness = new();

    public async Task<StorageFaultInjectionResult> ValidateAsync(
        ILocalEvidenceStore evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var result = await _harness.RunAsync(cancellationToken);
        await evidence.AppendEvidenceAsync($"storage-fault-harness:{result.ResultFingerprint}", result.ResultFingerprint, cancellationToken);
        await evidence.AppendEventAsync(
            $"event:storage-fault-harness:{result.ResultFingerprint}",
            "STORAGE_FAULT_VALIDATION",
            result.Passed ? "STORAGE_FAULT_HARNESS_COMPLETED" : "STORAGE_FAULT_HARNESS_FAILED",
            result.ResultFingerprint,
            cancellationToken);
        return result;
    }
}
