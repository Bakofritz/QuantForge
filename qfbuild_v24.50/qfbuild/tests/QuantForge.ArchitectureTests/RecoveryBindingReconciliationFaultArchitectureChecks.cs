namespace QuantForge.ArchitectureTests;

public static class RecoveryBindingReconciliationFaultArchitectureChecks
{
    public static IReadOnlyList<string> Validate()
    {
        var expected = new[] { "AuditWrite", "EvidenceWrite", "EventWrite", "RetryAfterFailure", "DuplicateReconciliation", "ConflictingReconciliation", "ContinueAfterUnrecordedReconciliation", "CrashAfterDetection" };
        var source = File.ReadAllText(Path.Combine("..", "..", "..", "src", "QuantForge.Runtime", "RecoveryBindingReconciliationFaultContracts.cs"));
        var harness = File.ReadAllText(Path.Combine("..", "..", "..", "src", "QuantForge.Runtime", "RecoveryBindingReconciliationFaultHarness.cs"));
        var runtime = File.ReadAllText(Path.Combine("..", "..", "..", "src", "QuantForge.Runtime", "ResearchRuntime.cs"));
        var failures = new List<string>();
        foreach (var name in expected) if (!source.Contains(name, StringComparison.Ordinal)) failures.Add($"MISSING_SCENARIO:{name}");
        if (!harness.Contains("RECOVERY_RECONCILIATION_NOT_DURABLE", StringComparison.Ordinal)) failures.Add("MISSING_FAIL_CLOSED_CODE");
        if (!harness.Contains("IDEMPOTENT_RETRY", StringComparison.Ordinal)) failures.Add("MISSING_IDEMPOTENT_RETRY");
        if (!harness.Contains("RECOVERY_BINDING_RECONCILIATION_CONFLICT", StringComparison.Ordinal)) failures.Add("MISSING_CONFLICT_GUARD");
        if (!runtime.Contains("ValidateRecoveryBindingReconciliationFaultsAsync", StringComparison.Ordinal)) failures.Add("MISSING_RUNTIME_ENTRY_POINT");
        return failures;
    }
}
