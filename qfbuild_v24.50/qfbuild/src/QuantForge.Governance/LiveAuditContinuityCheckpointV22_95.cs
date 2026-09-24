namespace QuantForge.Governance;

public sealed record LiveAuditContinuityCheckpointV22_95(
    string ChainHead,
    string VerifiedHead,
    string CheckpointId,
    DateTime PersistedAtUtc)
{
    public bool IsConsistent(DateTime nowUtc, TimeSpan maxAge) =>
        !string.IsNullOrWhiteSpace(ChainHead) &&
        ChainHead == VerifiedHead &&
        !string.IsNullOrWhiteSpace(CheckpointId) &&
        PersistedAtUtc <= nowUtc &&
        nowUtc - PersistedAtUtc <= maxAge;
}

public static class LiveAuditContinuityCheckpointGateV22_95
{
    public static bool CanResume(LiveAuditContinuityCheckpointV22_95 checkpoint, DateTime nowUtc, TimeSpan maxAge) =>
        checkpoint is not null && checkpoint.IsConsistent(nowUtc, maxAge);
}
