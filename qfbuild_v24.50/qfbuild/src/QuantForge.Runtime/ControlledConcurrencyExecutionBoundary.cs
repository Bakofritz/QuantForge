using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public sealed record ControlledConcurrencyExecutionRequest(
    string BatchId,
    string WorkerId,
    string WorkerFingerprint,
    int RequestedCases,
    int ConfiguredConcurrency,
    long EstimatedMemoryBytesPerCase,
    long MemoryBudgetBytes,
    bool NativeRuntimeQualified);

public sealed record ControlledConcurrencyExecutionDecision(
    bool Allowed,
    int EffectiveConcurrency,
    string? StopCode,
    string AdmissionFingerprint,
    string LifecycleFingerprint);

/// <summary>
/// Integrates native-runtime qualification, resource admission and the deterministic lifecycle.
/// It does not itself execute strategy code; it creates an explicit, auditable execution boundary.
/// </summary>
public sealed class ControlledConcurrencyExecutionBoundary
{
    private readonly ConcurrencyResourceGate _resourceGate = new();
    private readonly ControlledConcurrencyLifecycle _lifecycle = new();

    public ControlledConcurrencyExecutionDecision Evaluate(ControlledConcurrencyExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.BatchId)) throw new ArgumentException("Batch id is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.WorkerId)) throw new ArgumentException("Worker id is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.WorkerFingerprint)) throw new ArgumentException("Worker fingerprint is required.", nameof(request));

        var admission = _resourceGate.Evaluate(
            request.RequestedCases,
            request.ConfiguredConcurrency,
            request.EstimatedMemoryBytesPerCase,
            request.MemoryBudgetBytes,
            request.NativeRuntimeQualified);

        var lifecycleSeed = new AdmissionCaseLedger(
            "__batch_admission__",
            request.BatchId,
            AdmissionCaseState.Pending,
            request.WorkerFingerprint,
            0,
            0,
            null,
            null,
            string.Empty);
        var lifecycle = admission.Allowed
            ? _lifecycle.Start(_lifecycle.Admit(lifecycleSeed, request.WorkerFingerprint))
            : lifecycleSeed;
        var lifecycleFingerprint = lifecycle.StateFingerprint;
        return new(admission.Allowed, admission.EffectiveConcurrency, admission.StopCode, admission.Fingerprint, lifecycleFingerprint);
    }
}
