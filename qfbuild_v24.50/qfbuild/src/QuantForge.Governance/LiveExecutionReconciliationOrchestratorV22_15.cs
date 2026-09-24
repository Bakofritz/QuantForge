namespace QuantForge.Governance;

public sealed record ExecutionReconciliationDecisionV22_15(bool CanProceed, string Code, string Reason);

public sealed class LiveExecutionReconciliationOrchestratorV22_15
{
    public ExecutionReconciliationDecisionV22_15 Evaluate(
        bool brokerConnected,
        bool riskHealthy,
        bool auditHealthy,
        bool startupReady,
        bool unresolvedUnknownExecutions,
        bool authorityStillValid)
    {
        if (!startupReady) return new(false, "STARTUP_NOT_READY", "Startup integrity gate has not cleared.");
        if (!brokerConnected) return new(false, "BROKER_DISCONNECTED", "Broker transport is unavailable.");
        if (!riskHealthy) return new(false, "RISK_UNHEALTHY", "Risk health is not healthy.");
        if (!auditHealthy) return new(false, "AUDIT_INVALID", "Tamper-evident audit chain is not healthy.");
        if (unresolvedUnknownExecutions) return new(false, "UNKNOWN_EXECUTION_REQUIRES_RECONCILIATION", "An ambiguous execution outcome remains unresolved.");
        if (!authorityStillValid) return new(false, "AUTHORITY_REVALIDATION_FAILED", "Live authority changed or expired during recovery.");
        return new(true, "RECOVERY_CLEARED", "Recovery conditions are clear; the final live-order admission gate remains authoritative.");
    }
}
