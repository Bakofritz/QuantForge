using QuantForge.Storage;

namespace QuantForge.Core.Runtime;

/// <summary>Fail-closed reconciliation gate. It can validate a resolution record but never invokes a continuation callback.</summary>
public static class RecoveryContinuationResolutionGate
{
    public static string Validate(
        RecoveryContinuationResolutionRecord resolution,
        RecoveryContinuationReconciliationRecord decision,
        RecoveryContinuationExternalBoundary boundary,
        DateTimeOffset now)
    {
        if (!string.Equals(resolution.OperationFingerprint, decision.OperationFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException(RecoveryContinuationResolutionPolicy.StaleDecisionCode);
        if (!string.Equals(resolution.DecisionFingerprint, decision.DecisionFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException(RecoveryContinuationResolutionPolicy.StaleDecisionCode);
        if (!string.Equals(resolution.EvidenceFingerprint, boundary.EvidenceFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException(RecoveryContinuationResolutionPolicy.StaleDecisionCode);
        if (now >= resolution.ExpiresAt)
            throw new InvalidOperationException(RecoveryContinuationResolutionPolicy.ExpiredCode);
        if (resolution.Consumed)
            throw new InvalidOperationException("RECOVERY_CONTINUATION_RESOLUTION_ALREADY_CONSUMED");
        if (resolution.Action == RecoveryContinuationResolutionAction.RequestReplayAuthorization)
            throw new InvalidOperationException(RecoveryContinuationResolutionPolicy.ReplayRequiresExplicitBoundaryCode);
        return resolution.Action.ToString().ToUpperInvariant();
    }
}

public sealed record RecoveryContinuationExternalBoundary(
    string EvidenceFingerprint,
    bool AutomaticExecutionPermitted = false);
