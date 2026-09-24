using QuantForge.Core.Runtime;

namespace QuantForge.ArchitectureTests;

public static class RecoveryContinuationInvalidationArchitectureChecks
{
    public static void AssertArchitecture(string sourceRoot)
    {
        var guard = File.ReadAllText(Path.Combine(sourceRoot, "src/QuantForge.Runtime/RecoveryContinuationInvalidationGuard.cs"));
        var service = File.ReadAllText(Path.Combine(sourceRoot, "src/QuantForge.Runtime/CheckpointBoundContinuationService.cs"));
        var harness = File.ReadAllText(Path.Combine(sourceRoot, "src/QuantForge.Runtime/RecoveryContinuationInvalidationHarness.cs"));

        Require(guard.Contains("ValidateBeforeStepAsync", StringComparison.Ordinal), "BEFORE_STEP_GUARD_MISSING");
        Require(guard.Contains("ValidateBeforeCommitAsync", StringComparison.Ordinal), "BEFORE_COMMIT_GUARD_MISSING");
        Require(guard.Contains("LoadCancellationAsync", StringComparison.Ordinal), "CANCELLATION_RECHECK_MISSING");
        Require(guard.Contains("LoadLeaseAsync", StringComparison.Ordinal), "LEASE_RECHECK_MISSING");
        Require(guard.Contains("LoadLatestCheckpointAsync", StringComparison.Ordinal), "CHECKPOINT_RECHECK_MISSING");
        Require(guard.Contains("RECOVERY_CHECKPOINT_CHANGED_AFTER_AUTHORIZATION", StringComparison.Ordinal), "CHECKPOINT_INVALIDATION_CODE_MISSING");
        Require(service.Contains("ValidateBeforeStepAsync", StringComparison.Ordinal), "SERVICE_BEFORE_STEP_GUARD_NOT_USED");
        Require(service.Contains("ValidateBeforeCommitAsync", StringComparison.Ordinal), "SERVICE_BEFORE_COMMIT_GUARD_NOT_USED");
        Require(service.IndexOf("ValidateBeforeStepAsync", StringComparison.Ordinal) < service.IndexOf("executeStepAsync(current", StringComparison.Ordinal), "STEP_GUARD_MUST_PRECEDE_CALLBACK");
        Require(service.IndexOf("ValidateBeforeCommitAsync", StringComparison.Ordinal) < service.IndexOf("SaveCheckpointAsync", StringComparison.Ordinal), "COMMIT_GUARD_MUST_PRECEDE_CHECKPOINT_SAVE");
        Require(harness.Contains("LEASE_LOSS_BEFORE_STEP", StringComparison.Ordinal), "LEASE_LOSS_CASE_MISSING");
        Require(harness.Contains("CANCELLATION_BEFORE_STEP", StringComparison.Ordinal), "CANCELLATION_CASE_MISSING");
        Require(harness.Contains("CHECKPOINT_REPLACED_BEFORE_COMMIT", StringComparison.Ordinal), "CHECKPOINT_COMMIT_CASE_MISSING");
    }

    private static void Require(bool condition, string code)
    {
        if (!condition) throw new InvalidOperationException(code);
    }
}
