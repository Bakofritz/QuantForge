namespace QuantForge.Governance;

public sealed record StartupIntegrityResultV22_05(bool Ready, string Code, string Reason);

public sealed class LiveStartupIntegrityCoordinatorV22_05
{
    public StartupIntegrityResultV22_05 Evaluate(bool auditChainHealthy, bool executionStoreHealthy, LiveExecutionRecoveryStateV21_95 recovery)
    {
        if (!auditChainHealthy) return new(false, "AUDIT_CHAIN_INVALID", "Tamper-evident audit verification failed; live execution remains blocked.");
        if (!executionStoreHealthy) return new(false, "EXECUTION_STORE_UNAVAILABLE", "Execution-intent persistence is unavailable; live execution remains blocked.");
        if (recovery.Blocked) return new(false, recovery.Code, recovery.Reason);
        return new(true, "STARTUP_INTEGRITY_READY", "Startup integrity checks passed; final live authority checks are still required.");
    }
}
