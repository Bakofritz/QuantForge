namespace QuantForge.Governance;

public sealed record LiveRecoveryStatePersistenceV23_00(
    string ExecutionId,
    string IdempotencyKey,
    LiveRecoveryStageV22_55 Stage,
    string RequestFingerprint,
    string? BrokerOrderId,
    DateTime PersistedAtUtc)
{
    public bool IsRestorable(DateTime nowUtc, TimeSpan maxAge) =>
        !string.IsNullOrWhiteSpace(ExecutionId) &&
        !string.IsNullOrWhiteSpace(IdempotencyKey) &&
        !string.IsNullOrWhiteSpace(RequestFingerprint) &&
        PersistedAtUtc <= nowUtc &&
        nowUtc - PersistedAtUtc <= maxAge;
}

public static class LiveRecoveryStateRestorationGateV23_00
{
    public static bool CanRestore(LiveRecoveryStatePersistenceV23_00 state, DateTime nowUtc, TimeSpan maxAge) =>
        state is not null && state.IsRestorable(nowUtc,maxAge);

    public static bool RequiresReconciliation(LiveRecoveryStatePersistenceV23_00 state) =>
        state.Stage is LiveRecoveryStageV22_55.Unknown or LiveRecoveryStageV22_55.ReconcileRequired;
}
