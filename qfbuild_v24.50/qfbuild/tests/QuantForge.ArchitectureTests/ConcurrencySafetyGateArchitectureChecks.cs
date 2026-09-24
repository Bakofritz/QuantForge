using QuantForge.Core.Runtime;

namespace QuantForge.ArchitectureTests;

public static class ConcurrencySafetyGateArchitectureChecks
{
    public static void Run()
    {
        if (typeof(ConcurrencyResourceBudget).GetMethod(nameof(ConcurrencyResourceBudget.Validate)) is null)
            throw new InvalidOperationException("Concurrency resource budget validation is required.");
        if (ConcurrencyContentionDomain.DefaultDomains.Count == 0)
            throw new InvalidOperationException("Shared-resource contention domains are required.");
        if (typeof(ConcurrencySafetyGateResult).GetProperty(nameof(ConcurrencySafetyGateResult.ParallelExecutionEnabled)) is null)
            throw new InvalidOperationException("Safety gate must expose an explicit parallel execution gate.");
        if (typeof(ConcurrencySafetyGateResult).GetProperty(nameof(ConcurrencySafetyGateResult.GateFingerprint)) is null)
            throw new InvalidOperationException("Safety gate must be fingerprinted.");
    }
}
