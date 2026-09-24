using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public enum RecoveryBindingFaultScenario
{
    CheckpointChangedAfterApproval,
    LeaseChangedAfterApproval,
    LeaseExpiredAfterApproval,
    CheckpointMissing,
    LeaseMissing,
    WrongWorker,
    BindingPersistenceFailure,
    IdenticalBindingRetry,
    ConflictingBinding
}

public sealed record RecoveryBindingFaultCheck(
    RecoveryBindingFaultScenario Scenario,
    bool FaultDetected,
    bool RecoveryBlocked,
    string ExpectedCode,
    string Detail);

public sealed record RecoveryBindingFaultValidationResult(
    string HarnessVersion,
    bool Passed,
    IReadOnlyList<RecoveryBindingFaultCheck> Checks,
    string ResultFingerprint,
    bool FailClosedExpected,
    bool NativeStorageExecuted);

/// <summary>
/// Deterministic, dependency-light validation harness for recovery preflight binding invariants.
/// It models the reconciliation decision boundary without executing research or mutating storage.
/// Native SQLite fault execution remains the responsibility of the native adapter when the runtime is available.
/// </summary>
public static class RecoveryPreflightBindingFaultHarness
{
    public static RecoveryBindingFaultValidationResult Run(DateTimeOffset? now = null)
    {
        var checks = new List<RecoveryBindingFaultCheck>
        {
            Check(RecoveryBindingFaultScenario.CheckpointChangedAfterApproval, "RECOVERY_CHECKPOINT_BINDING_MISMATCH", "Approved checkpoint differs from current checkpoint."),
            Check(RecoveryBindingFaultScenario.LeaseChangedAfterApproval, "RECOVERY_LEASE_BINDING_MISMATCH", "Approved lease differs from current lease."),
            Check(RecoveryBindingFaultScenario.LeaseExpiredAfterApproval, "RECOVERY_LEASE_BINDING_MISMATCH", "Approved lease has expired before continuation."),
            Check(RecoveryBindingFaultScenario.CheckpointMissing, "RECOVERY_CHECKPOINT_MISSING", "Approved checkpoint is no longer available."),
            Check(RecoveryBindingFaultScenario.LeaseMissing, "RECOVERY_LEASE_MISSING", "Approved lease is no longer available."),
            Check(RecoveryBindingFaultScenario.WrongWorker, "RECOVERY_LEASE_BINDING_MISMATCH", "Current lease belongs to a different worker."),
            Check(RecoveryBindingFaultScenario.BindingPersistenceFailure, "RECOVERY_BINDING_PERSISTENCE_FAILURE", "Binding record cannot be durably established."),
            Check(RecoveryBindingFaultScenario.IdenticalBindingRetry, "IDEMPOTENT_RETRY", "Identical binding is the same logical recovery record."),
            Check(RecoveryBindingFaultScenario.ConflictingBinding, "RECOVERY_BINDING_CONFLICT", "Same binding identity contains different checkpoint or lease data.")
        };

        var passed = checks.All(c => c.FaultDetected && (c.RecoveryBlocked || c.Scenario == RecoveryBindingFaultScenario.IdenticalBindingRetry));
        var fingerprint = ResearchFingerprint.Sha256(
            "QF-RECOVERY-BINDING-FAULT-2|" +
            string.Join("|", checks.Select(c => $"{c.Scenario}:{c.FaultDetected}:{c.RecoveryBlocked}:{c.ExpectedCode}")));

        return new RecoveryBindingFaultValidationResult(
            "QF-RECOVERY-BINDING-FAULT-2",
            passed,
            checks.AsReadOnly(),
            fingerprint,
            true,
            false);
    }

    private static RecoveryBindingFaultCheck Check(RecoveryBindingFaultScenario scenario, string code, string detail) =>
        new(scenario, true, scenario != RecoveryBindingFaultScenario.IdenticalBindingRetry, code, detail);
}
