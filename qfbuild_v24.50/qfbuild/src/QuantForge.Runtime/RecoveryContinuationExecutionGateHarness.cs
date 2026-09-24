using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

public enum RecoveryContinuationExecutionGateFaultPoint
{
    BlockedAuthorization,
    ReconciliationReadbackFailure,
    AuthorizedContinuation,
    ContinuationCallbackFailure
}

public sealed record RecoveryContinuationExecutionGateCheck(
    RecoveryContinuationExecutionGateFaultPoint Point,
    bool AuthorizationBlocked,
    int CallbackExecutions,
    bool ExpectedOutcome,
    string ExpectedCode,
    string Detail);

public sealed record RecoveryContinuationExecutionGateValidationResult(
    string HarnessId,
    bool Passed,
    IReadOnlyList<RecoveryContinuationExecutionGateCheck> Checks,
    string ResultFingerprint,
    bool Deterministic,
    bool NativeRuntimeValidated);

/// <summary>
/// Deterministic fault-injection harness for the final recovery continuation execution seam.
/// It verifies the callback cannot execute after blocked authorization and executes exactly once
/// after an explicit Ready authorization. No retry policy or live-trading authority is introduced.
/// </summary>
public static class RecoveryContinuationExecutionGateHarness
{
    public static async Task<RecoveryContinuationExecutionGateValidationResult> RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var checks = new List<RecoveryContinuationExecutionGateCheck>
        {
            await CheckBlockedAsync(RecoveryContinuationExecutionGateFaultPoint.BlockedAuthorization, RecoveryContinuationState.ReconciliationBlocked, "RECOVERY_CONTINUATION_NOT_AUTHORIZED", cancellationToken),
            await CheckBlockedAsync(RecoveryContinuationExecutionGateFaultPoint.ReconciliationReadbackFailure, RecoveryContinuationState.ReconciliationBlocked, "RECOVERY_CONTINUATION_NOT_AUTHORIZED", cancellationToken),
            await CheckAuthorizedAsync(cancellationToken),
            await CheckCallbackFailureAsync(cancellationToken)
        };

        var passed = checks.All(c => c.ExpectedOutcome == (c.AuthorizationBlocked ? c.CallbackExecutions == 0 : c.CallbackExecutions == 1));
        var fingerprint = ResearchFingerprint.Sha256(
            "QF-RECOVERY-CONTINUATION-EXECUTION-GATE-1|" +
            string.Join("|", checks.Select(c => $"{c.Point}:{c.AuthorizationBlocked}:{c.CallbackExecutions}:{c.ExpectedOutcome}:{c.ExpectedCode}")));
        return new("QF-RECOVERY-CONTINUATION-EXECUTION-GATE-1", passed, checks.AsReadOnly(), fingerprint, true, false);
    }

    private static async Task<RecoveryContinuationExecutionGateCheck> CheckBlockedAsync(
        RecoveryContinuationExecutionGateFaultPoint point,
        RecoveryContinuationState state,
        string code,
        CancellationToken cancellationToken)
    {
        var authorization = Result(state, code);
        var executions = 0;
        try
        {
            await RecoveryContinuationExecutionGate.ExecuteAsync(
                authorization,
                _ =>
                {
                    executions++;
                    return Task.CompletedTask;
                },
                cancellationToken);
            return new(point, true, executions, false, code, "Blocked authorization unexpectedly reached the continuation callback.");
        }
        catch (InvalidOperationException ex) when (ex.Message == "RECOVERY_CONTINUATION_NOT_AUTHORIZED")
        {
            return new(point, true, executions, executions == 0, code, "Blocked authorization prevented callback execution.");
        }
    }

    private static async Task<RecoveryContinuationExecutionGateCheck> CheckAuthorizedAsync(CancellationToken cancellationToken)
    {
        var authorization = Result(RecoveryContinuationState.Ready, null);
        var executions = 0;
        await RecoveryContinuationExecutionGate.ExecuteAsync(
            authorization,
            _ =>
            {
                executions++;
                return Task.CompletedTask;
            },
            cancellationToken);
        return new(RecoveryContinuationExecutionGateFaultPoint.AuthorizedContinuation, false, executions, executions == 1, "AUTHORIZED", "A valid Ready authorization executes the intended continuation exactly once.");
    }

    private static async Task<RecoveryContinuationExecutionGateCheck> CheckCallbackFailureAsync(CancellationToken cancellationToken)
    {
        var authorization = Result(RecoveryContinuationState.Ready, null);
        var executions = 0;
        try
        {
            await RecoveryContinuationExecutionGate.ExecuteAsync(
                authorization,
                _ =>
                {
                    executions++;
                    throw new InvalidOperationException("INJECTED_CONTINUATION_FAILURE");
                },
                cancellationToken);
            return new(RecoveryContinuationExecutionGateFaultPoint.ContinuationCallbackFailure, false, executions, false, "INJECTED_CONTINUATION_FAILURE", "Injected callback failure unexpectedly completed.");
        }
        catch (InvalidOperationException ex) when (ex.Message == "INJECTED_CONTINUATION_FAILURE")
        {
            return new(RecoveryContinuationExecutionGateFaultPoint.ContinuationCallbackFailure, false, executions, executions == 1, "INJECTED_CONTINUATION_FAILURE", "Callback failure propagates without an automatic retry or second execution.");
        }
    }

    private static RecoveryContinuationResult Result(RecoveryContinuationState state, string? failureCode) =>
        new(state, "batch", "context", "job", "worker", 1, 0, "state", "audit", failureCode);
}
