namespace QuantForge.ArchitectureTests;

public static class RecoveryContinuationExecutionGateArchitectureChecks
{
    public static IReadOnlyList<string> Run()
    {
        var gate = File.ReadAllText(FindSource("RecoveryContinuationExecutionGate.cs"));
        var harness = File.ReadAllText(FindSource("RecoveryContinuationExecutionGateHarness.cs"));
        var continuation = File.ReadAllText(FindSource("CheckpointBoundContinuationService.cs"));
        var checks = new List<string>();
        Require(gate.Contains("RecoveryContinuationState.Ready", StringComparison.Ordinal), checks, "Execution-side gate requires explicit Ready authorization.");
        Require(gate.Contains("RECOVERY_CONTINUATION_NOT_AUTHORIZED", StringComparison.Ordinal), checks, "Blocked authorization has an explicit fail-closed code.");
        Require(harness.Contains("CallbackExecutions", StringComparison.Ordinal), checks, "Fault harness counts continuation callback executions.");
        Require(harness.Contains("ReconciliationReadbackFailure", StringComparison.Ordinal), checks, "Fault harness includes reconciliation read-back failure coverage.");
        Require(harness.Contains("executions == 1", StringComparison.Ordinal), checks, "Fault harness asserts exactly one authorized callback execution.");
        Require(continuation.Contains("RecoveryContinuationExecutionGate.ExecuteAsync", StringComparison.Ordinal), checks, "Checkpoint continuation uses the execution-side authorization seam.");
        Require(continuation.Contains("RecoveryContinuationGateService", StringComparison.Ordinal), checks, "Checkpoint continuation remains behind the durable recovery authorization gate.");
        return checks;
    }

    private static void Require(bool condition, List<string> checks, string description)
    {
        if (!condition) throw new InvalidOperationException("ARCHITECTURE_CHECK_FAILED: " + description);
        checks.Add("PASS: " + description);
    }

    private static string FindSource(string name)
    {
        var root = Directory.GetCurrentDirectory();
        var found = Directory.EnumerateFiles(root, name, SearchOption.AllDirectories).FirstOrDefault();
        if (found is null) throw new FileNotFoundException(name);
        return found;
    }
}
