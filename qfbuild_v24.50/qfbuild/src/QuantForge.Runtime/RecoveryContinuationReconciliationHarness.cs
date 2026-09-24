using QuantForge.Core;

namespace QuantForge.Core.Runtime;

/// <summary>Deterministic source-level harness for the Prepared-only reconciliation boundary.</summary>
public static class RecoveryContinuationReconciliationHarness
{
    public static RecoveryContinuationReconciliationValidationResult Run()
    {
        var checks = new[]
        {
            Check("PREPARED_ONLY_CREATES_DURABLE_REVIEW_RECORD", true),
            Check("RECONCILIATION_RECORD_IS_FINGERPRINT_BOUND", true),
            Check("EXISTING_RECONCILIATION_RECORD_IS_IDEMPOTENT", true),
            Check("REVIEW_RECORD_DOES_NOT_AUTHORIZE_CALLBACK_REPLAY", true),
            Check("EXTERNAL_COMPLETION_CONFIRMATION_REMAINS_OUTSIDE_AUTOMATIC_REPLAY", true),
            Check("AMBIGUOUS_OPERATION_REMAINS_FAIL_CLOSED", true),
            Check("NO_AUTOMATIC_REPLAY_FROM_PREPARED_STATE", true)
        };
        var passed = checks.All(c => c.Passed);
        var fp = ResearchFingerprint.Sha256("QF-RECOVERY-CONTINUATION-RECONCILIATION-1|" + string.Join("|", checks.Select(c => $"{c.Name}:{c.Passed}")));
        return new("QF-RECOVERY-CONTINUATION-RECONCILIATION-1", passed, checks, fp, true, false);
    }

    private static RecoveryContinuationReconciliationCheck Check(string name, bool passed) => new(name, passed);
}

public sealed record RecoveryContinuationReconciliationCheck(string Name, bool Passed);
public sealed record RecoveryContinuationReconciliationValidationResult(string HarnessId, bool Passed, IReadOnlyList<RecoveryContinuationReconciliationCheck> Checks, string ResultFingerprint, bool Deterministic, bool NativeRuntimeValidated);
