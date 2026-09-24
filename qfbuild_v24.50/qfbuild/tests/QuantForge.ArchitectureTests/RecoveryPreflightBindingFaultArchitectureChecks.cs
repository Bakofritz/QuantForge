using QuantForge.Core.Runtime;

namespace QuantForge.ArchitectureTests;

public static class RecoveryPreflightBindingFaultArchitectureChecks
{
    public static void Validate()
    {
        var result = RecoveryPreflightBindingFaultHarness.Run();
        if (!result.Passed) throw new InvalidOperationException("Recovery binding fault harness must pass all deterministic scenarios.");
        if (!result.FailClosedExpected) throw new InvalidOperationException("Recovery binding fault validation must be fail-closed.");
        if (result.NativeStorageExecuted) throw new InvalidOperationException("Dependency-light binding harness must not claim native storage execution.");
        if (result.Checks.Count != 9) throw new InvalidOperationException("Expected nine recovery binding scenarios.");
        if (!result.Checks.Any(c => c.Scenario == RecoveryBindingFaultScenario.IdenticalBindingRetry && !c.RecoveryBlocked))
            throw new InvalidOperationException("Identical binding retry must remain idempotent.");
        if (!result.Checks.Where(c => c.Scenario != RecoveryBindingFaultScenario.IdenticalBindingRetry).All(c => c.RecoveryBlocked))
            throw new InvalidOperationException("Conflicting recovery binding scenarios must block continuation.");
    }
}
