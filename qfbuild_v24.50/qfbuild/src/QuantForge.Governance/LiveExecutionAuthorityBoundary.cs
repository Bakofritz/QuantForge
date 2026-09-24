namespace QuantForge.Governance;

public sealed record LiveExecutionAuthorityDecision(bool Allowed, string Code, string Reason);

/// <summary>Explicit final handoff boundary. It consumes a confirmed token but deliberately does not submit an order.</summary>
public sealed class LiveExecutionAuthorityBoundary
{
    public LiveExecutionAuthorityDecision AuthorizeBrokerHandoff(
        LiveOrderConfirmationResult confirmation,
        LiveOrderPreview preview,
        LiveOrderAdmissionRequest currentAdmission)
    {
        if (!confirmation.Confirmed || confirmation.Token is null)
            return new(false, "CONFIRMATION_REQUIRED", "A valid human confirmation is required before broker handoff.");
        if (!confirmation.Token.Consumed)
            return new(false, "CONFIRMATION_NOT_CONSUMED", "The confirmation token must be consumed by the confirmation service.");
        if (!string.Equals(confirmation.Token.PreviewId, preview.PreviewId, StringComparison.Ordinal))
            return new(false, "PREVIEW_MISMATCH", "The confirmed preview does not match the requested broker handoff.");
        var admission = new LiveOrderAdmissionGate().Evaluate(currentAdmission);
        return admission.Allowed
            ? new(true, "BROKER_HANDOFF_AUTHORIZED", "All confirmation and final admission controls passed. This boundary still does not submit an order.")
            : new(false, "ADMISSION_RECHECK_FAILED", admission.Code);
    }
}
