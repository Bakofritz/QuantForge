namespace QuantForge.Governance;

public sealed record LiveStartupRecoveryReplayMatrixV23_05(
    bool SecureRuntimeUsable,
    bool AuditContinuityCurrent,
    bool BrokerSessionValid,
    bool BrokerHeartbeatHealthy,
    bool RecoveryStateRestorable,
    bool AuthorityValid,
    bool RiskHealthy,
    bool UnresolvedUnknownExecution,
    bool LiveArmed)
{
    public bool CanResume() =>
        SecureRuntimeUsable && AuditContinuityCurrent && BrokerSessionValid &&
        BrokerHeartbeatHealthy && RecoveryStateRestorable && AuthorityValid &&
        RiskHealthy && !UnresolvedUnknownExecution && LiveArmed;
}

public static class LiveStartupRecoveryReplayGateV23_05
{
    public static bool Evaluate(LiveStartupRecoveryReplayMatrixV23_05 state) => state is not null && state.CanResume();
}
