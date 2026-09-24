using QuantForge.Core.Runtime;

namespace QuantForge.ArchitectureTests;

public static class ConcurrencyValidationHarnessChecks
{
    public static void Run()
    {
        if (typeof(ConcurrencyValidationHarness).GetMethod(nameof(ConcurrencyValidationHarness.RunAsync)) is null)
            throw new InvalidOperationException("Concurrency validation harness entry point is missing.");
        if (typeof(ConcurrencyValidationResult).GetProperty(nameof(ConcurrencyValidationResult.ResultFingerprint)) is null)
            throw new InvalidOperationException("Concurrency validation results must be fingerprinted.");
        if (!string.Equals(new ConcurrencyValidationHarness().GetType().Namespace, "QuantForge.Core.Runtime", StringComparison.Ordinal))
            throw new InvalidOperationException("Concurrency harness namespace is unexpected.");
    }
}
