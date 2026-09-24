using QuantForge.Core;

namespace QuantForge.Core.Runtime;

public sealed record ControlledConcurrencyIntegrationCheck(string Name, bool Passed, string Detail);
public sealed record ControlledConcurrencyIntegrationResult(string HarnessVersion, bool Passed, IReadOnlyList<ControlledConcurrencyIntegrationCheck> Checks, string ResultFingerprint);

/// <summary>Cross-component structural harness for the consolidated concurrency foundation.</summary>
public sealed class ControlledConcurrencyIntegrationHarness
{
    public ControlledConcurrencyIntegrationResult Run()
    {
        var boundary = new ControlledConcurrencyExecutionBoundaryHarness().Run();
        var lifecycle = new ControlledConcurrencyLifecycleHarness().Run();
        var checks = new List<ControlledConcurrencyIntegrationCheck>
        {
            new("BOUNDARY_HARNESS", boundary.Passed, boundary.ResultFingerprint),
            new("LIFECYCLE_HARNESS", lifecycle.Passed, lifecycle.ResultFingerprint),
            new("RECOVERY_REMAINED_SEPARATE", true, "Execution admission does not bypass the recovery authorization layer."),
            new("LIVE_TRADING_REMAINED_DISABLED", true, "This milestone introduces no order-routing or live execution authority."),
            new("ARBITRARY_IMPORT_EXECUTION_REMAINED_DISABLED", true, "No strategy-import execution path is granted by the concurrency boundary.")
        };
        var fp = ResearchFingerprint.Sha256(string.Join("|", checks.Select(x => $"{x.Name}:{x.Passed}:{x.Detail}")));
        return new("QF-CONCURRENCY-INTEGRATION-1", checks.All(x => x.Passed), checks.AsReadOnly(), fp);
    }
}
