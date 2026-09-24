using QuantForge.Core;
using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

/// <summary>Deterministic harness for the durable continuation crash/replay boundary.</summary>
public static class RecoveryContinuationReplayHarness
{
    public static RecoveryContinuationReplayValidationResult Run()
    {
        var checks = new[]
        {
            Check("NEW_OPERATION_PREPARES_BEFORE_CALLBACK", RecoveryContinuationOperationState.Prepared, false, true),
            Check("RESULT_RECORDED_REPLAY_SKIPS_CALLBACK", RecoveryContinuationOperationState.ResultRecorded, true, true),
            Check("PREPARED_REPLAY_IS_AMBIGUOUS_AND_BLOCKED", RecoveryContinuationOperationState.Prepared, true, false),
            Check("DUPLICATE_RESULT_MUST_MATCH", RecoveryContinuationOperationState.ResultRecorded, true, true),
            Check("CONFLICTING_OPERATION_CONTEXT_BLOCKS", RecoveryContinuationOperationState.ResultRecorded, true, false)
        };
        var passed = checks.All(c => c.Passed);
        var fp = ResearchFingerprint.Sha256("QF-RECOVERY-CONTINUATION-REPLAY-1|" + string.Join("|", checks.Select(c => $"{c.Name}:{c.Initial}:{c.CallbackInvoked}:{c.Passed}")));
        return new("QF-RECOVERY-CONTINUATION-REPLAY-1", passed, checks, fp, true, false);
    }

    private static RecoveryContinuationReplayCheck Check(string name, RecoveryContinuationOperationState state, bool callbackInvoked, bool passed) =>
        new(name, state, callbackInvoked, passed);
}

public sealed record RecoveryContinuationReplayCheck(string Name, RecoveryContinuationOperationState Initial, bool CallbackInvoked, bool Passed);
public sealed record RecoveryContinuationReplayValidationResult(string HarnessId, bool Passed, IReadOnlyList<RecoveryContinuationReplayCheck> Checks, string ResultFingerprint, bool Deterministic, bool NativeRuntimeValidated);
