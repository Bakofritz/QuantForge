namespace QuantForge.Core.Runtime;

public sealed record ConcurrencyResourceGateResult(
    bool Allowed,
    int RequestedCases,
    int EffectiveConcurrency,
    long EstimatedMemoryBytes,
    long MemoryBudgetBytes,
    string? StopCode,
    string Fingerprint);

/// <summary>
/// Admission-control gate. It does not start work; it decides whether a batch may be admitted.
/// Conservative behavior is mandatory when resource estimates are unavailable or exceed budget.
/// </summary>
public sealed class ConcurrencyResourceGate
{
    public ConcurrencyResourceGateResult Evaluate(
        int requestedCases,
        int configuredConcurrency,
        long estimatedMemoryBytesPerCase,
        long memoryBudgetBytes,
        bool nativeRuntimeQualified)
    {
        if (requestedCases < 1) throw new ArgumentOutOfRangeException(nameof(requestedCases));
        if (configuredConcurrency < 1) throw new ArgumentOutOfRangeException(nameof(configuredConcurrency));
        if (estimatedMemoryBytesPerCase < 0) throw new ArgumentOutOfRangeException(nameof(estimatedMemoryBytesPerCase));
        if (memoryBudgetBytes <= 0) throw new ArgumentOutOfRangeException(nameof(memoryBudgetBytes));

        var effective = Math.Min(requestedCases, configuredConcurrency);
        var estimated = checked(estimatedMemoryBytesPerCase * effective);
        string? stop = null;
        var allowed = nativeRuntimeQualified;
        if (!nativeRuntimeQualified) stop = "NATIVE_RUNTIME_NOT_QUALIFIED";
        else if (estimatedMemoryBytesPerCase == 0) { allowed = false; stop = "RESOURCE_ESTIMATE_UNAVAILABLE"; }
        else if (estimated > memoryBudgetBytes) { allowed = false; stop = "MEMORY_BUDGET_EXCEEDED"; }

        var fp = QuantForge.Core.ResearchFingerprint.Sha256($"{requestedCases}|{configuredConcurrency}|{estimatedMemoryBytesPerCase}|{memoryBudgetBytes}|{nativeRuntimeQualified}|{effective}|{estimated}|{allowed}|{stop}");
        return new(allowed, requestedCases, allowed ? effective : 0, estimated, memoryBudgetBytes, stop, fp);
    }
}
