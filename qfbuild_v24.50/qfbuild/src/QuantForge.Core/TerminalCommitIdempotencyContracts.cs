namespace QuantForge.Core;

public sealed class TerminalCommitConflictException : InvalidOperationException
{
    public TerminalCommitConflictException(string code, string message) : base(message) => Code = code;
    public string Code { get; }
}

public static class TerminalCommitRules
{
    public static bool IsTerminal(string state) =>
        string.Equals(state, nameof(DurableJobState.Completed), StringComparison.Ordinal) ||
        string.Equals(state, nameof(DurableJobState.Failed), StringComparison.Ordinal) ||
        string.Equals(state, nameof(DurableJobState.Canceled), StringComparison.Ordinal);

    public static bool ReceiptMatches(TerminalCommitReceipt existing, TerminalCommitReceipt incoming) =>
        string.Equals(existing.ReceiptFingerprint, incoming.ReceiptFingerprint, StringComparison.Ordinal) &&
        string.Equals(existing.BatchId, incoming.BatchId, StringComparison.Ordinal) &&
        string.Equals(existing.ContextId, incoming.ContextId, StringComparison.Ordinal) &&
        string.Equals(existing.JobId, incoming.JobId, StringComparison.Ordinal) &&
        string.Equals(existing.WorkerId, incoming.WorkerId, StringComparison.Ordinal) &&
        string.Equals(existing.WorkerFingerprint, incoming.WorkerFingerprint, StringComparison.Ordinal) &&
        string.Equals(existing.State, incoming.State, StringComparison.Ordinal) &&
        string.Equals(existing.ResultFingerprint, incoming.ResultFingerprint, StringComparison.Ordinal) &&
        existing.StartedAt == incoming.StartedAt && existing.CompletedAt == incoming.CompletedAt &&
        existing.ElapsedMilliseconds == incoming.ElapsedMilliseconds &&
        existing.HeartbeatRenewals == incoming.HeartbeatRenewals &&
        string.Equals(existing.ResourceStatus, incoming.ResourceStatus, StringComparison.Ordinal) &&
        string.Equals(existing.ErrorCode, incoming.ErrorCode, StringComparison.Ordinal);
}
