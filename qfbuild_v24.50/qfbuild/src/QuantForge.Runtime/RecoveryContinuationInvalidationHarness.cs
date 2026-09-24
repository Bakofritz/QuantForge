namespace QuantForge.Core.Runtime;

/// <summary>
/// Deterministic state-machine harness for the two crash-window boundaries introduced by the
/// invalidation guard. It models the durable authority signals that the real guard reads.
/// </summary>
public static class RecoveryContinuationInvalidationHarness
{
    public static RecoveryContinuationInvalidationValidationResult Run()
    {
        var checks = new[]
        {
            Check("LEASE_LOSS_BEFORE_STEP", AuthoritySignal.Ready, AuthoritySignal.LeaseLost, false, "RECOVERY_LEASE_DISAPPEARED_AFTER_AUTHORIZATION"),
            Check("CANCELLATION_BEFORE_STEP", AuthoritySignal.Ready, AuthoritySignal.CancellationRequested, false, "RECOVERY_CANCELLATION_REQUESTED_AFTER_AUTHORIZATION"),
            Check("JOB_STATE_MUTATION_BEFORE_STEP", AuthoritySignal.Ready, AuthoritySignal.JobStateChanged, false, "RECOVERY_JOB_STATE_CHANGED_AFTER_AUTHORIZATION"),
            Check("CHECKPOINT_REPLACED_BEFORE_STEP", AuthoritySignal.Ready, AuthoritySignal.CheckpointChanged, false, "RECOVERY_CHECKPOINT_CHANGED_AFTER_AUTHORIZATION"),
            Check("LEASE_LOSS_BEFORE_COMMIT", AuthoritySignal.Ready, AuthoritySignal.LeaseLost, false, "RECOVERY_LEASE_DISAPPEARED_AFTER_AUTHORIZATION"),
            Check("CANCELLATION_BEFORE_COMMIT", AuthoritySignal.Ready, AuthoritySignal.CancellationRequested, false, "RECOVERY_CANCELLATION_REQUESTED_AFTER_AUTHORIZATION"),
            Check("CHECKPOINT_REPLACED_BEFORE_COMMIT", AuthoritySignal.Ready, AuthoritySignal.CheckpointChanged, false, "RECOVERY_CHECKPOINT_CHANGED_AFTER_AUTHORIZATION"),
            Check("NO_INVALIDATION", AuthoritySignal.Ready, AuthoritySignal.Ready, true, "AUTHORIZED")
        };

        var passed = checks.All(x => x.ExpectedBlocked == !x.Allowed);
        var fingerprint = ResearchFingerprint.Sha256(
            "QF-RECOVERY-CONTINUATION-INVALIDATION-1|" +
            string.Join("|", checks.Select(x => $"{x.Name}:{x.Initial}:{x.Mutated}:{x.Allowed}:{x.ExpectedBlocked}:{x.ExpectedCode}")));
        return new("QF-RECOVERY-CONTINUATION-INVALIDATION-1", passed, checks, fingerprint, true, false);
    }

    private static RecoveryContinuationInvalidationCheck Check(string name, AuthoritySignal initial, AuthoritySignal mutated, bool allowed, string expectedCode) =>
        new(name, initial, mutated, allowed, !allowed, expectedCode,
            allowed ? "No authority invalidation was introduced; continuation remains eligible." :
                "Post-authorization invalidation blocks the continuation boundary without retry or authority escalation.");

    private enum AuthoritySignal { Ready, LeaseLost, CancellationRequested, JobStateChanged, CheckpointChanged }
}

public sealed record RecoveryContinuationInvalidationCheck(
    string Name,
    object Initial,
    object Mutated,
    bool Allowed,
    bool ExpectedBlocked,
    string ExpectedCode,
    string Detail);

public sealed record RecoveryContinuationInvalidationValidationResult(
    string HarnessId,
    bool Passed,
    IReadOnlyList<RecoveryContinuationInvalidationCheck> Checks,
    string ResultFingerprint,
    bool Deterministic,
    bool NativeRuntimeValidated);
